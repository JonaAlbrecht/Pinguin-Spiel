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

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
GOLDEN = ROOT / "golden"


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


def main() -> None:
    mats = settlement.load_materials(DATA / "Materials/materials_settlement_v1.json")
    level = settlement.load_level(DATA / "Levels/MG1_Huddle_Settlement.json")
    out = GOLDEN / "thermal"
    out.mkdir(parents=True, exist_ok=True)
    for name, tiles in settlement_cases(level, mats):
        t_first = settlement.solve(tiles, level, mats)
        res = settlement.run(tiles, level, mats)
        doc = {
            "case": name,
            "level": "MG1_Huddle_Settlement",
            "materials": "materials_settlement_v1",
            "tolerance": 1e-6,
            "tiles": tiles,
            "shielded": [[int(v) for v in row] for row in
                         settlement.shielded_mask(tiles, mats, level.wind_direction)],
            "firstSolve": [round(float(v), 10) for v in t_first.ravel()],
            "finalSolve": [round(float(v), 10) for v in res.temperatures.ravel()],
            "finalTiles": res.final_tiles,
            "meltPasses": [len(p) for p in res.melt_passes],
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


if __name__ == "__main__":
    main()
