"""Python-Referenz für MG1 Teil B „Die erste Siedlung“ (stationäre Wärmebilanz auf dem Raster).

Modell (identisch zum C#-Core, siehe docs/04 Abschnitt 4.3 und specs/001-mg1-huddle/spec.md):

    0 = q_i - h_i (T_i - T_luft) + sum_N k_ij (T_j - T_i)

    k_ij  = harmonisches Mittel der Leitwerte beider Zellen
    h_i   = h(Material) * (Wind / Windreferenz) * (Abschirmfaktor, falls hinter einem Schneewall)
    q_i   = Wärmequelle des Materials (Laterne: level.lantern.heat, Iglu: kleine Eigenwärme)

Schmelz-Kaskade: Iglus mit T >= Schmelzgrenze werden zu Wasser, dann neu lösen (max. maxMeltPasses).
"""
from __future__ import annotations

import json
import math
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np
import scipy.sparse as sp
import scipy.sparse.linalg as spla

WIND_DIRECTIONS = {  # Richtung, in die der Wind weht (x = Osten, y = Süden / Zeile nach unten)
    "N": (0.0, 1.0), "NE": (-1.0, 1.0), "E": (-1.0, 0.0), "SE": (-1.0, -1.0),
    "S": (0.0, -1.0), "SW": (1.0, -1.0), "W": (1.0, 0.0), "NW": (1.0, 1.0),
}


@dataclass
class Material:
    key: str
    symbol: str
    k: float
    h: float
    q: float


@dataclass
class MaterialSet:
    id: str
    shield_range: float
    shield_cone: float
    shield_factor: float
    wind_reference: float
    materials: list[Material]

    def by_symbol(self, s: str) -> Material:
        for m in self.materials:
            if m.symbol == s:
                return m
        raise KeyError(f"Unbekanntes Zellsymbol {s!r}")


@dataclass
class Level:
    id: str
    width: int
    height: int
    air_temp: float
    wind: float
    wind_direction: str
    lantern_heat: float
    lantern_cost: int
    wall_limit: int
    comfort_min: float
    melt: float
    igloo_target: int
    budget: int
    rounds: int
    max_melt_passes: int
    initial_tiles: list[str]
    kalle_tiles: list[str] = field(default_factory=list)
    hint_tiles: list[str] = field(default_factory=list)


def load_materials(path: Path) -> MaterialSet:
    d = json.loads(Path(path).read_text(encoding="utf-8"))
    return MaterialSet(
        id=d["id"], shield_range=d["shieldRange"], shield_cone=d["shieldCone"],
        shield_factor=d["shieldFactor"], wind_reference=d["windReference"],
        materials=[Material(m["key"], m["symbol"], m["k"], m["h"], m["q"]) for m in d["materials"]],
    )


def load_level(path: Path) -> Level:
    d = json.loads(Path(path).read_text(encoding="utf-8"))
    return Level(
        id=d["id"], width=d["grid"]["width"], height=d["grid"]["height"],
        air_temp=d["climate"]["airTemp"], wind=d["climate"]["wind"],
        wind_direction=d["climate"]["windDirection"],
        lantern_heat=d["lantern"]["heat"], lantern_cost=d["lantern"]["cost"],
        wall_limit=d["wallLimit"], comfort_min=d["goals"]["iglooComfortMin"],
        melt=d["goals"]["iglooMelt"], igloo_target=d["goals"]["iglooTarget"],
        budget=d["budget"], rounds=d["rounds"], max_melt_passes=d["maxMeltPasses"],
        initial_tiles=d["initialTiles"], kalle_tiles=d.get("kalleTiles", []),
        hint_tiles=d.get("hintTiles", []),
    )


def shielded_mask(tiles: list[str], mats: MaterialSet, wind_direction: str) -> np.ndarray:
    """True für Zellen, die im Windschatten eines Schneewalls ('W') liegen."""
    h, w = len(tiles), len(tiles[0])
    dx, dy = WIND_DIRECTIONS[wind_direction]
    n = math.hypot(dx, dy)
    dx, dy = dx / n, dy / n
    walls = [(x, y) for y in range(h) for x in range(w) if tiles[y][x] == "W"]
    mask = np.zeros((h, w), dtype=bool)
    r = mats.shield_range
    for y in range(h):
        for x in range(w):
            for wx, wy in walls:
                vx, vy = x - wx, y - wy
                dist = math.hypot(vx, vy)
                if dist == 0.0 or dist > r:
                    continue
                if (vx * dx + vy * dy) / dist >= mats.shield_cone:
                    mask[y, x] = True
                    break
    return mask


def assemble(tiles: list[str], level: Level, mats: MaterialSet):
    """Baut A und b für A·T = b (A symmetrisch positiv definit)."""
    h, w = len(tiles), len(tiles[0])
    n = w * h
    shield = shielded_mask(tiles, mats, level.wind_direction)
    wind_factor = level.wind / mats.wind_reference
    k = np.empty(n)
    hh = np.empty(n)
    q = np.empty(n)
    for y in range(h):
        for x in range(w):
            i = y * w + x
            m = mats.by_symbol(tiles[y][x])
            k[i] = m.k
            hi = m.h * wind_factor
            if shield[y, x]:
                hi *= mats.shield_factor
            hh[i] = hi
            q[i] = level.lantern_heat if m.symbol == "L" else m.q
    rows, cols, vals = [], [], []
    diag = hh.copy()
    for y in range(h):
        for x in range(w):
            i = y * w + x
            for nx, ny in ((x + 1, y), (x, y + 1)):
                if nx >= w or ny >= h:
                    continue
                j = ny * w + nx
                kij = 2.0 * k[i] * k[j] / (k[i] + k[j])
                diag[i] += kij
                diag[j] += kij
                rows += [i, j]
                cols += [j, i]
                vals += [-kij, -kij]
    rows += list(range(n))
    cols += list(range(n))
    vals += list(diag)
    a = sp.csr_matrix((vals, (rows, cols)), shape=(n, n))
    b = q + hh * level.air_temp
    return a, b


def solve(tiles: list[str], level: Level, mats: MaterialSet) -> np.ndarray:
    a, b = assemble(tiles, level, mats)
    t = spla.spsolve(a.tocsc(), b)
    return t.reshape(len(tiles), len(tiles[0]))


@dataclass
class Result:
    temperatures: np.ndarray
    final_tiles: list[str]
    melt_passes: list[list[tuple[int, int]]]
    igloos_total: int
    igloos_comfortable: int
    igloos_cold: int
    igloos_melted: int
    lanterns: int
    walls: int
    cost: int
    stars: int


def run(tiles: list[str], level: Level, mats: MaterialSet) -> Result:
    tiles = [str(r) for r in tiles]
    original_igloos = sum(r.count("I") for r in tiles)
    passes: list[list[tuple[int, int]]] = []
    t = solve(tiles, level, mats)
    for _ in range(level.max_melt_passes):
        melted = [(x, y) for y, r in enumerate(tiles) for x, c in enumerate(r)
                  if c == "I" and t[y, x] >= level.melt]
        if not melted:
            break
        passes.append(melted)
        rows = [list(r) for r in tiles]
        for x, y in melted:
            rows[y][x] = "~"
        tiles = ["".join(r) for r in rows]
        t = solve(tiles, level, mats)
    comfortable = cold = 0
    for y, r in enumerate(tiles):
        for x, c in enumerate(r):
            if c == "I":
                if t[y, x] >= level.melt:
                    pass  # nach max. Durchläufen noch zu warm: zählt als geschmolzen-gefährdet, nicht gemütlich
                elif t[y, x] >= level.comfort_min:
                    comfortable += 1
                else:
                    cold += 1
    melted_total = sum(len(p) for p in passes)
    lanterns = sum(r.count("L") for r in tiles)
    walls = sum(r.count("W") for r in tiles)
    cost = lanterns * level.lantern_cost
    stars = 0
    if comfortable >= level.igloo_target and cost <= level.budget:
        stars = 1
        if melted_total == 0:
            stars = 2
            if cost <= 0.8 * level.budget:
                stars = 3
    return Result(t, tiles, passes, original_igloos, comfortable, cold, melted_total,
                  lanterns, walls, cost, stars)


def with_overrides(level: Level, overrides: dict | None) -> Level:
    """Kopie des Levels mit geänderten Werten, z. B. {"wind": 2.0} für „Was wäre, wenn …“ (MG1 Schritt 4).

    Schlüssel wie im Level-JSON (camelCase), damit Golden Files und Leveldaten dieselben Namen nutzen.
    """
    if not overrides:
        return level
    names = {"wind": "wind", "windDirection": "wind_direction", "maxMeltPasses": "max_melt_passes",
             "airTemp": "air_temp", "budget": "budget"}
    from dataclasses import replace
    unknown = set(overrides) - set(names)
    if unknown:
        raise KeyError(f"Unbekannte Level-Überschreibung: {sorted(unknown)}")
    return replace(level, **{names[k]: v for k, v in overrides.items()})


@dataclass
class Balance:
    """Wärmebilanz je Zelle im gelösten Zustand – die „Waage“ eines Iglus (MG1 Schritt 5).

    Im stationären Zustand gilt je Zelle:  source + from_neighbours = to_neighbours + to_air.
    Alle Werte sind >= 0; die Richtung steckt im Namen.
    """
    source: np.ndarray
    from_neighbours: np.ndarray
    to_neighbours: np.ndarray
    to_air: np.ndarray


def balance(tiles: list[str], level: Level, mats: MaterialSet, t: np.ndarray) -> Balance:
    h, w = len(tiles), len(tiles[0])
    shield = shielded_mask(tiles, mats, level.wind_direction)
    wind_factor = level.wind / mats.wind_reference
    tf = np.asarray(t, dtype=float).ravel()
    n = w * h
    k = np.empty(n)
    source = np.empty(n)
    to_air = np.empty(n)
    for y in range(h):
        for x in range(w):
            i = y * w + x
            m = mats.by_symbol(tiles[y][x])
            k[i] = m.k
            hi = m.h * wind_factor * (mats.shield_factor if shield[y, x] else 1.0)
            source[i] = level.lantern_heat if m.symbol == "L" else m.q
            to_air[i] = hi * (tf[i] - level.air_temp)
    gain = np.zeros(n)
    loss = np.zeros(n)
    for y in range(h):
        for x in range(w):
            i = y * w + x
            for nx, ny in ((x + 1, y), (x, y + 1)):
                if nx >= w or ny >= h:
                    continue
                j = ny * w + nx
                flow = 2.0 * k[i] * k[j] / (k[i] + k[j]) * (tf[j] - tf[i])  # > 0: Wärme fließt von j nach i
                if flow > 0:
                    gain[i] += flow
                    loss[j] += flow
                else:
                    gain[j] -= flow
                    loss[i] -= flow
    return Balance(source, gain, loss, to_air)
