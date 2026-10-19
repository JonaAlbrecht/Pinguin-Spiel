"""Golden Files für MG3 „Fischfabrik“ (golden/process/), aufgerufen von generate_golden.py.

- pcg32.json        Zufallszahlen, Exponential- und Dreieckswerte (bitgenau)
- lesson.json       die Rechentafel aus Schritt 2, Ereignis für Ereignis
- layout.json       Wege (Übergabe, Förderband, Robo-Robbe), Kosten und fehlende Teile je Aufbau
- trace_<fall>.json jedes Ereignis kleiner Fälle (Zeit, Art, zwei Kennzahlen) – fängt jede Abweichung in der
                    Reihenfolge der Ereignisse
- day_<aufbau>.json Kennzahlen einer ganzen Schicht und von 10 Tagen (Mittelwert, Vertrauensbereich, Sterne)
"""
from __future__ import annotations

import json
from dataclasses import replace
from pathlib import Path

from simref import process as p
from simref import process_lesson
from simref.process_random import Pcg32, exponential, triangular

LEVEL = "Levels/MG3_Fischfabrik.json"


def r10(v: float) -> float:
    return round(float(v), 10)


def layouts(level: p.Level) -> dict[str, list[str]]:
    """Aufbauten, die das Spiel zeigt, plus Grenzfälle für einzelne Regeln."""
    raw = level.raw
    return {
        "olaf": raw["olafTiles"],
        "kalle": level.kalle_tiles,
        "hint": level.hint_tiles,
        # Eine Station von jeder Sorte dicht an dicht, Band zur Verladung: der Engpass Filetieren bleibt
        "compact": ["D..........T", "D..........T", "DSFP..#....T", "D..BBBBBBBBT", "D.K...#....T", "D..........T",
                    "D..........T"],
        # Zwei Robo-Robben: zu wenig zum Tragen
        "two_robots": ["D..........T", "D..S.....P.T", "D.....#....T", "D....KFF...T", "D....K#....T", "D...RR.....T",
                       "D..........T"],
    }


def write(path: Path, doc: dict) -> None:
    path.write_text(json.dumps(doc, indent=1), encoding="utf-8")


def summary_doc(s: p.Summary, level: p.Level) -> dict:
    return {
        "delivered": [d.delivered for d in s.days],
        "spoiled": [d.spoiled for d in s.days],
        "stolen": [d.stolen for d in s.days],
        "meanDelivered": r10(s.mean_delivered), "sdDelivered": r10(s.sd_delivered),
        "halfWidth": r10(s.half_width), "lower": r10(s.lower),
        "meanSpoiled": r10(s.mean_spoiled), "meanLeadTime": r10(s.mean_lead_time),
        "stars": p.stars(s, level), "lights": list(p.lights(s, level)),
    }


def day_doc(d: p.DayResult) -> dict:
    return {
        "delivered": d.delivered, "finished": d.finished, "arrived": d.arrived, "stolen": d.stolen,
        "spoiled": d.spoiled, "leadTime": r10(d.lead_time), "utilization": [r10(u) for u in d.utilization],
        "maxQueue": d.max_queue, "endQueue": d.end_queue, "bottleneck": d.bottleneck, "events": d.events,
    }


def trace_cases(level: p.Level) -> dict[str, tuple[p.FactoryConfig, int, float]]:
    """Kleine Fälle, Ereignis für Ereignis: (Aufbau, Seed, Ende)."""
    lay = layouts(level)
    olaf = p.to_config(lay["olaf"], level)
    # Störungen sehr häufig: Ausfall mitten in der Arbeit, veraltete Fertig-Meldung, Reparatur und Weiterarbeit
    stormy = replace(olaf, stages=[replace(s, mtbf=25.0, mttr=6.0) for s in olaf.stages])
    # Kühlregal mit nur einem Platz: was nicht hineinpasst, verdirbt
    tiny_regal = replace(olaf, stages=[replace(s, capacity=1 if s.capacity >= 0 else s.capacity) for s in olaf.stages])
    # Viele Möwen
    gulls = replace(olaf, gull_chance=0.3)
    return {
        "olaf": (olaf, level.seed, 120.0),
        "kalle": (p.to_config(lay["kalle"], level), level.seed, 120.0),
        "hint": (p.to_config(lay["hint"], level), level.seed, 120.0),
        "failures": (stormy, 11, 150.0),
        "tiny_regal": (tiny_regal, 5, 120.0),
        "gulls": (gulls, 3, 90.0),
    }


def generate(golden_dir: Path, data_dir: Path) -> None:
    out = golden_dir / "process"
    out.mkdir(parents=True, exist_ok=True)
    level = p.load_level(data_dir / LEVEL)

    # Zufallszahlen
    cases = []
    for seed, stream in ((42, 54), (2026, 1), (0, 0), (2**63 + 5, 999)):
        rng = Pcg32(seed, stream)
        uints = [rng.next_uint() for _ in range(8)]
        rng = Pcg32(seed, stream)
        exps = [r10(exponential(rng, 20.0)) for _ in range(8)]
        rng = Pcg32(seed, stream)
        tris = [r10(triangular(rng, 4.0, 6.0, 10.0)) for _ in range(8)]
        cases.append({"seed": str(seed), "stream": stream, "uint": uints, "exponential20": exps,
                      "triangular4_6_10": tris})
    write(out / "pcg32.json", {"case": "pcg32", "tolerance": 1e-12, "cases": cases})

    # Rechentafel
    lesson = process_lesson.load(data_dir / LEVEL)
    rows = process_lesson.run(lesson)
    write(out / "lesson.json", {"case": "lesson", "tolerance": 1e-9, "rows": [
        {"time": r10(r.time), "kind": r.kind, "before": r.before, "arrived": r.arrived, "finished": r.finished,
         "after": r.after, "waiting": r.waiting} for r in rows]})
    print("process lesson:", [r.after for r in rows])

    # Wege, Kosten, fehlende Teile
    lay = dict(layouts(level))
    lay["empty"] = level.initial_tiles
    lay["no_robots"] = ["D..........T", "D..S.....P.T", "D.....#....T", "D....KF....T", "D.....#....T",
                        "D..........T", "D..........T"]
    lay["belt_detour"] = ["D..........T", "DSFP.......T", "D..B..#....T", "D..B.......T", "D..BBBBBBBBT",
                          "D.K...#....T", "D..........T"]
    entries = {}
    for name, rows_ in lay.items():
        hs = p.hops(rows_, level)
        entries[name] = {
            "tiles": rows_,
            "cost": p.cost(rows_, level),
            "problems": [list(x) for x in p.problems(rows_, level)],
            "hops": [None if h is None else {"mode": h.mode, "time": r10(h.time), "distance": h.distance} for h in hs],
        }
    write(out / "layout.json", {"case": "layout", "tolerance": 1e-9, "layouts": entries})

    # Ereignisse kleiner Fälle
    for name, (cfg, seed, end) in trace_cases(level).items():
        sim = p.FactorySimulation(cfg, seed, trace=True)
        sim.run_until(end)
        write(out / f"trace_{name}.json", {
            "case": name, "seed": seed, "end": end, "tolerance": 1e-9,
            "trace": [[r10(t), k, a, b] for t, k, a, b in sim.trace],
            "result": day_doc(sim.result()),
        })
        print(f"process trace_{name}: {len(sim.trace)} Ereignisse")

    # Ganze Schicht und 10 Tage je Aufbau
    for name, rows_ in layouts(level).items():
        cfg = p.to_config(rows_, level)
        first = p.run_day(cfg, level.seed)
        s = p.run_days(cfg, level.seed, level.replications)
        write(out / f"day_{name}.json", {
            "case": name, "tiles": rows_, "seed": level.seed, "replications": level.replications,
            "tolerance": 1e-9, "day": day_doc(first), "days": summary_doc(s, level)})
        print(f"process day_{name}: Schnitt {s.mean_delivered:.1f} (Untergrenze {s.lower:.1f}), "
              f"verdorben {s.mean_spoiled:.1f}, Sterne {p.stars(s, level)}")
