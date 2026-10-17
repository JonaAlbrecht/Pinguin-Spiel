"""Erzeugt die Golden Files in ../golden/ (Referenzergebnisse für die C#-Tests).

Aufruf:  python generate_golden.py
"""
from __future__ import annotations

import json
import math
import random
from pathlib import Path

import numpy as np

from simref import huddle, settlement
from simref.cases import MELT_TWO_PASSES, SINGLE_WALL

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
GOLDEN = ROOT / "golden"
LEVEL_JSON = json.loads((DATA / "Levels/MG1_Huddle_Settlement.json").read_text(encoding="utf-8"))


def random_layout(seed: int, lanterns: int, igloos: int, walls: int) -> list[str]:
    rnd = random.Random(seed)
    g = [["."] * 24 for _ in range(24)]
    free = [(x, y) for y in range(24) for x in range(24)]
    rnd.shuffle(free)
    for sym, n in (("L", lanterns), ("I", igloos), ("W", walls)):
        for _ in range(n):
            x, y = free.pop()
            g[y][x] = sym
    return ["".join(r) for r in g]


def settlement_cases(level, mats):
    yield "hint", level.hint_tiles
    yield "kalle", level.kalle_tiles
    yield "empty", level.initial_tiles
    for seed in (1, 2, 3):
        yield f"random_{seed}", random_layout(seed, 9, 16, 8)
    # Schneewall-Abschirmung: Mauer im Nordwesten vor einem Laternen-Iglu-Block
    g = [list(r) for r in level.hint_tiles]
    for i in range(6, 14):
        g[5][i] = "W"
    yield "hint_with_walls", ["".join(r) for r in g]
    # Ab hier Fälle aus dem Review zu PR #2: Regeln, die vorher kein Golden File traf.
    yield "melt_two_passes", MELT_TWO_PASSES
    yield "melt_cap_1", MELT_TWO_PASSES, {"maxMeltPasses": 1}
    g = [list(r) for r in level.hint_tiles]
    g[0][0] = "L"
    g[23][23] = "L"
    yield "over_budget", ["".join(r) for r in g]
    for direction in settlement.WIND_DIRECTIONS:
        yield f"wind_{direction}", SINGLE_WALL, {"windDirection": direction}
    what_if = LEVEL_JSON["whatIf"]["noWind"]
    yield "whatif_real", what_if["tiles"]
    yield "whatif_no_wind", what_if["tiles"], what_if["overrides"]


def main() -> None:
    mats = settlement.load_materials(DATA / "Materials/materials_settlement_v1.json")
    level = settlement.load_level(DATA / "Levels/MG1_Huddle_Settlement.json")
    out = GOLDEN / "thermal"
    out.mkdir(parents=True, exist_ok=True)
    for name, tiles, *rest in settlement_cases(level, mats):
        overrides = rest[0] if rest else {}
        lv = settlement.with_overrides(level, overrides)
        t_first = settlement.solve(tiles, lv, mats)
        res = settlement.run(tiles, lv, mats)
        bal = settlement.balance(res.final_tiles, lv, mats, res.temperatures)
        doc = {
            "case": name,
            "level": "MG1_Huddle_Settlement",
            "materials": "materials_settlement_v1",
            "overrides": overrides,
            "tolerance": 1e-6,
            "tiles": tiles,
            "shielded": [[int(v) for v in row] for row in
                         settlement.shielded_mask(tiles, mats, lv.wind_direction)],
            "firstSolve": [round(float(v), 10) for v in t_first.ravel()],
            "finalSolve": [round(float(v), 10) for v in res.temperatures.ravel()],
            "finalTiles": res.final_tiles,
            "meltPasses": [len(p) for p in res.melt_passes],
            # Waage je Zelle im Endzustand (Reihenfolge wie finalSolve)
            "balance": {
                "source": [round(float(v), 10) for v in bal.source],
                "fromNeighbours": [round(float(v), 10) for v in bal.from_neighbours],
                "toNeighbours": [round(float(v), 10) for v in bal.to_neighbours],
                "toAir": [round(float(v), 10) for v in bal.to_air],
            },
            "result": {
                "igloosTotal": res.igloos_total, "comfortable": res.igloos_comfortable,
                "cold": res.igloos_cold, "melted": res.igloos_melted,
                "lanterns": res.lanterns, "walls": res.walls, "cost": res.cost, "stars": res.stars,
            },
        }
        (out / f"settlement_{name}.json").write_text(json.dumps(doc, indent=1), encoding="utf-8")
        print(f"settlement_{name}: {doc['result']} passes={doc['meltPasses']}")

    hp = huddle.load_params(DATA / "Levels/MG1A_LastHuddle.json")
    rnd = random.Random(7)
    pos = np.array([[rnd.uniform(-2, 2), rnd.uniform(-2, 2)] for _ in range(12)])
    temps = np.full(12, 25.0)
    winds = [(1.0, 0.0)] * 30 + [(0.0, -1.0)] * 30 + [(-math.sqrt(0.5), math.sqrt(0.5))] * 30
    history = []
    for w in winds:
        temps = huddle.step(temps, pos, w, hp, 1.0)
        history.append([round(float(v), 10) for v in temps])
    doc = {
        "case": "huddle_random_7",
        "tolerance": 1e-6,
        "dt": 1.0,
        "startTemp": 25.0,
        "positions": [[round(float(a), 10), round(float(b), 10)] for a, b in pos],
        "windTo": [list(w) for w in winds],
        "exposedAtStart": [int(v) for v in huddle.exposure(pos, winds[0], hp)],
        "temperatures": history,
    }
    (out / "huddle_random_7.json").write_text(json.dumps(doc, indent=1), encoding="utf-8")
    print("huddle_random_7: final", [round(v, 2) for v in history[-1]])

    # Zwei-Pinguine-Beispiel vor dem Huddle: Temperaturen UND die Zerlegung jedes Schritts,
    # denn genau diese Zahlen zeigt das Spiel in der Rechenregel.
    pos, temps, wind, dt, steps = huddle.pair_lesson(DATA / "Levels/MG1A_LastHuddle.json")
    rows = []
    for _ in range(steps):
        after = huddle.step(temps, pos, wind, hp, dt)
        t = huddle.step_terms(temps, after, pos, wind, hp, dt)
        rows.append({
            "before": [round(float(v), 10) for v in temps],
            "after": [round(float(v), 10) for v in after],
            "own": [round(float(v), 10) for v in t.own],
            "fromNeighbours": [round(float(v), 10) for v in t.from_neighbours],
            "toWind": [round(float(v), 10) for v in t.to_wind],
        })
        temps = after
    doc = {"case": "huddle_pair", "tolerance": 1e-6, "dt": dt, "windTo": list(wind),
           "positions": [[float(a), float(b)] for a, b in pos], "steps": rows}
    (out / "huddle_pair.json").write_text(json.dumps(doc, indent=1), encoding="utf-8")
    print("huddle_pair: Ende", [round(v, 2) for v in temps])


if __name__ == "__main__":
    main()
