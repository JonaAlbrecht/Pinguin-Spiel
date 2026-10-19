"""MG2 „Polar-Express“: Zug im Längsschnitt, Plätze, Luftwiderstand und Fahrzeit (Python-Referenz).

Geometrie in „Fein-Einheiten“ (1 = ein Kästchen der feinen Auflösung 160 × 64). Gröbere Auflösungen rastern
dieselbe Form mit Faktor ``scale`` (Kästchen-Mitte unter der Dachlinie → Zug).

Zug: steht auf dem glatten Boden (y = 0), Höhe ``height``, von ``start`` bis ``start + length``.
- Nase: kubische Bézier-Kurve aus 4 Ziehpunkten P0…P3 (Dachlinie). P0 liegt auf der Stirnseite (x = start, nur die
  Höhe ist frei), P3 auf dem Dach (y = height, nur die Länge ist frei), P1 und P2 frei. Die x-Werte steigen an,
  darum ist die Dachlinie eine Funktion von x (Halbierungssuche – in C# identisch).
- Heck: Spiegelbild der Nase (ein Schnellzug fährt in beide Richtungen).
- Wagenübergänge: offen ragen Kupplung und Faltenbalg kantig über das Dach (bis ``transitionHeight``),
  verkleidet ist das Dach dort glatt. (Eine Lücke nach unten hätte bei der kleinen Reynolds-Zahl keinen messbaren
  Einfluss – geprüft 19.10.2026 –, ein kantiger Wulst schon.)

Plätze: Spalten (Fein-Einheiten), in denen der Zug mindestens ``headroom`` × Höhe hat und kein Übergang liegt;
je ``columnsPerRow`` Spalten eine Sitzreihe mit ``seatsPerRow`` Plätzen. Hängt nicht von der Auflösung ab.

Strömung: gleiche Reynolds-Zahl in jeder Auflösung (nu = u · Zughöhe in Kästchen / Re), gleiche Zeit
(Schritte proportional zur Feinheit). Widerstandsbeiwert = mittlere Kraft / (½ · u² · Zughöhe in Kästchen).

Fahrzeit: konstante Zugkraft F gegen Rollwiderstand R und Luftwiderstand C·(v + w)² (w = Gegenwind),
C = ``dragScale`` · Widerstandsbeiwert. Gleichgewicht F = R + C (v + w)² → v = sqrt((F - R) / C) - w
(mindestens ``minSpeed``), Fahrzeit = Strecke / v in Spiel-Stunden.
Vereinfachung (ehrlich benannt): Der Beiwert stammt aus einer Strömung mit kleiner Reynolds-Zahl.
"""
from __future__ import annotations

import json
import math
from dataclasses import dataclass, field
from pathlib import Path

import numpy as np

from simref import flow


@dataclass
class Resolution:
    id: str
    scale: float

    def size(self, level: "Level") -> tuple[int, int]:
        return round(level.channel_length * self.scale), round(level.channel_height * self.scale)


@dataclass
class Level:
    channel_length: float
    channel_height: float
    resolutions: list[Resolution]
    play_resolution: str
    u: float
    reynolds: float
    steps: int
    average_steps: int
    ramp: int
    start: float
    length: float
    height: float
    nose_max: float
    tip_min: float
    transitions: list[float]
    transition_width: float
    transition_height: float
    headroom: float
    columns_per_row: int
    seats_per_row: int
    passengers: int
    traction: float
    rolling: float
    headwind: float
    drag_scale: float
    distance: float
    min_speed: float
    target_time: float
    best_time: float
    rounds: int
    shapes: dict = field(default_factory=dict)
    lesson: dict = field(default_factory=dict)

    def resolution(self, rid: str) -> Resolution:
        return next(r for r in self.resolutions if r.id == rid)


def load(path: Path) -> Level:
    d = json.loads(Path(path).read_text(encoding="utf-8"))
    fl, tr, se, tv, go = d["flow"], d["train"], d["seats"], d["travel"], d["goals"]
    return Level(
        d["channel"]["length"], d["channel"]["height"],
        [Resolution(r["id"], r["scale"]) for r in d["resolutions"]], d["playResolution"],
        fl["u"], fl["reynolds"], fl["steps"], fl["averageSteps"], fl["ramp"],
        tr["start"], tr["length"], tr["height"], tr["noseMax"], tr["tipMin"],
        tr["transitions"], tr["transitionWidth"], tr["transitionHeight"],
        se["headroom"], se["columnsPerRow"], se["seatsPerRow"], se["passengers"],
        tv["traction"], tv["rolling"], tv["headwind"], tv["dragScale"], tv["distance"], tv["minSpeed"],
        go["targetTime"], go["bestTime"], d["rounds"], d["shapes"], d["lesson"])


@dataclass
class Shape:
    """Vier Ziehpunkte der Nase (Fein-Einheiten) und welche Wagenübergänge verkleidet sind."""
    points: list[tuple[float, float]]
    covered: list[bool]


def shape_from(d: dict) -> Shape:
    return Shape([(float(p[0]), float(p[1])) for p in d["nose"]], [bool(c) for c in d["covered"]])


def clamp(v: float, lo: float, hi: float) -> float:
    return lo if v < lo else hi if v > hi else v


def clamp_shape(s: Shape, lv: Level) -> Shape:
    """Ziehpunkte in den erlaubten Bereich: P0 auf der Stirn, P3 auf dem Dach, x aufsteigend, alles im Nasenbereich."""
    x0, h = lv.start, lv.height
    (_, p0y), (p1x, p1y), (p2x, p2y), (p3x, _) = s.points
    p3x = clamp(p3x, x0 + 1.0, x0 + lv.nose_max)
    p1x = clamp(p1x, x0, p3x)
    p2x = clamp(p2x, p1x, p3x)
    return Shape([(x0, clamp(p0y, lv.tip_min, h)), (p1x, clamp(p1y, 0.0, h)), (p2x, clamp(p2y, 0.0, h)), (p3x, h)],
                 list(s.covered))


def bezier(p, t: float) -> tuple[float, float]:
    u = 1.0 - t
    a, b, c, d = u * u * u, 3.0 * u * u * t, 3.0 * u * t * t, t * t * t
    return (a * p[0][0] + b * p[1][0] + c * p[2][0] + d * p[3][0],
            a * p[0][1] + b * p[1][1] + c * p[2][1] + d * p[3][1])


def nose_height(p, x: float) -> float:
    """Höhe der Dachlinie an der Stelle x (P0.x ≤ x ≤ P3.x) – 60 Halbierungen, wie in C#."""
    lo, hi = 0.0, 1.0
    for _ in range(60):
        mid = 0.5 * (lo + hi)
        if bezier(p, mid)[0] < x:
            lo = mid
        else:
            hi = mid
    return bezier(p, 0.5 * (lo + hi))[1]


def in_transition(lv: Level, x: float) -> int:
    """Index des Wagenübergangs an der Stelle x, sonst -1."""
    for i, g in enumerate(lv.transitions):
        if lv.start + g <= x < lv.start + g + lv.transition_width:
            return i
    return -1


def profile(s: Shape, lv: Level, x: float) -> float:
    """Höhe des Zuges an der Stelle x (0 = kein Zug)."""
    x0, x1 = lv.start, lv.start + lv.length
    if x < x0 or x >= x1:
        return 0.0
    t = in_transition(lv, x)
    if t >= 0:
        return lv.height if s.covered[t] else lv.transition_height
    p = s.points
    nose_end = p[3][0]
    if x < nose_end:
        return nose_height(p, x)
    mirrored = x0 + (x1 - x)
    if mirrored < nose_end:
        return nose_height(p, mirrored)
    return lv.height


def mask(s: Shape, lv: Level, res: Resolution) -> np.ndarray:
    """Hindernis-Maske (ny × nx, Zeile 0 unten): Kästchen-Mitte unter der Dachlinie → Zug."""
    nx, ny = res.size(lv)
    m = np.zeros((ny, nx), dtype=bool)
    for i in range(nx):
        h = profile(s, lv, (i + 0.5) / res.scale)
        for j in range(ny):
            if (j + 0.5) / res.scale < h:
                m[j, i] = True
    return m


def mask_rows(m: np.ndarray) -> list[str]:
    """Maske als Text, oberste Zeile zuerst (so, wie man den Zug sieht)."""
    return ["".join("#" if v else "." for v in row) for row in m[::-1]]


def seats(s: Shape, lv: Level) -> int:
    cols = 0
    for i in range(int(round(lv.start)), int(round(lv.start + lv.length))):
        x = i + 0.5
        if in_transition(lv, x) < 0 and profile(s, lv, x) >= lv.headroom * lv.height:
            cols += 1
    return (cols // lv.columns_per_row) * lv.seats_per_row


def tau(lv: Level, res: Resolution) -> float:
    nu = lv.u * lv.height * res.scale / lv.reynolds
    return 0.5 + 3.0 * nu


def steps(lv: Level, res: Resolution) -> tuple[int, int, int]:
    """Schritte gesamt, davon gemittelt, Anlauf – proportional zur Feinheit (gleiche Zeit in jeder Auflösung)."""
    return round(lv.steps * res.scale), round(lv.average_steps * res.scale), round(lv.ramp * res.scale)


@dataclass
class FlowResult:
    drag: float               # Widerstandsbeiwert (bezogen auf die Zughöhe), gemittelt
    history: list[float]      # Widerstandskraft je Schritt
    lbm: flow.Lbm


def make_lbm(s: Shape, lv: Level, res: Resolution) -> flow.Lbm:
    nx, ny = res.size(lv)
    _, _, ramp = steps(lv, res)
    return flow.Lbm(nx, ny, tau(lv, res), mask(s, lv, res), u_in=lv.u, ramp=ramp)


def simulate(s: Shape, lv: Level, res: Resolution) -> FlowResult:
    lbm = make_lbm(s, lv, res)
    total, avg, _ = steps(lv, res)
    hist = []
    acc = 0.0
    for k in range(total):
        lbm.step()
        hist.append(lbm.force[0])
        if k >= total - avg:
            acc += lbm.force[0]
    cd = (acc / avg) / (0.5 * lv.u * lv.u * lv.height * res.scale)
    return FlowResult(cd, hist, lbm)


def speed(cd: float, lv: Level) -> float:
    c = lv.drag_scale * cd
    v = math.sqrt(max(lv.traction - lv.rolling, 0.0) / c) - lv.headwind
    return max(v, lv.min_speed)


def travel_time(cd: float, lv: Level) -> float:
    return lv.distance / speed(cd, lv)


def stars(time: float, seat_count: int, lv: Level) -> int:
    """★ Küste in der Ziel-Fahrzeit, ★★ dazu Plätze für alle, ★★★ dazu Bestzeit-Bereich."""
    if time > lv.target_time:
        return 0
    if seat_count < lv.passengers:
        return 1
    return 3 if time <= lv.best_time else 2
