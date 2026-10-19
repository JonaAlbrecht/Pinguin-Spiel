"""Erzeugt die Golden Files in ../golden/ (Referenzergebnisse für die C#-Tests).

Aufruf:  python generate_golden.py
"""
from __future__ import annotations

import json
import random
from pathlib import Path

from simref import settlement, settlement_pair, settlement_wave
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

    # Zwei-Felder-Beispiel am Ende von Schritt 2: Temperaturen UND die Zerlegung jedes Schritts,
    # denn genau diese Zahlen zeigt das Spiel auf der Rechentafel.
    pair = settlement_pair.load(DATA / "Levels/MG1_Huddle_Settlement.json")
    temps = settlement_pair.start(pair)
    rows = []
    for _ in range(pair.steps):
        after = settlement_pair.step(temps, pair)
        t = settlement_pair.step_terms(after, pair)
        rows.append({
            "before": [round(float(v), 10) for v in temps],
            "after": [round(float(v), 10) for v in after],
            "own": [round(float(v), 10) for v in t.own],
            "fromNeighbour": [round(float(v), 10) for v in t.from_neighbour],
            "toWind": [round(float(v), 10) for v in t.to_wind],
        })
        temps = after
    doc = {"case": "pair_settlement", "tolerance": 1e-6, "dt": pair.dt, "steps": rows,
           "steady": [round(float(v), 10) for v in settlement_pair.steady(pair)]}
    (out / "pair_settlement.json").write_text(json.dumps(doc, indent=1), encoding="utf-8")
    print("pair_settlement: Ende", [round(v, 2) for v in temps], "Gleichgewicht", doc["steady"])
    # Wärmewelle in Schritt 3: erster und letzter Schritt vollständig, dazu Laterne und Iglus in jedem Schritt.
    wave = settlement_wave.load(DATA / "Levels/MG1_Huddle_Settlement.json")
    history = settlement_wave.run(wave, level, mats)
    width = len(wave.tiles[0])
    marked = [y * width + x for y, r in enumerate(wave.tiles) for x, c in enumerate(r) if c in "IL"]
    doc = {"case": "wave_settlement", "tolerance": 1e-6, "cells": marked,
           "steps": [[round(float(h.ravel()[i]), 10) for i in marked] for h in history],
           "first": [round(float(v), 10) for v in history[0].ravel()],
           "last": [round(float(v), 10) for v in history[-1].ravel()]}
    (out / "wave_settlement.json").write_text(json.dumps(doc, indent=1), encoding="utf-8")
    print("wave_settlement: Ende", doc["steps"][-1])

if __name__ == "__main__":
    main()
