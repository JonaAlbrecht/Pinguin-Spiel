"""Golden Files für MG2 „Polar-Express“ (golden/flow/*.json) – von generate_golden.py automatisch aufgerufen.

- flow_lesson: die Rechentafel (fünf Kästchen) Schritt für Schritt, genau die Zahlen, die das Spiel zeigt.
- flow_<form>_<auflösung>: Hindernis-Maske, Widerstandsbeiwert, Kraftverlauf, Geschwindigkeitsfeld am Ende,
  Plätze, Fahrzeit, Sterne. Kalle und die Startnase in allen drei Auflösungen (Schritt 3 „Genau hinschauen kostet
  Zeit“), dazu die Beispielnase und eine lange Spitznase fein.
- flow_travel: Fahrzeit-Modell für eine Reihe von Beiwerten (inkl. „bleibt fast stehen“).
Toleranz 1e-4 relativ (docs/04 Abschnitt 4.4: viele Zeitschritte).
"""
from __future__ import annotations

import json
from pathlib import Path

from simref import flow, flow_train as ft

TOL = 1e-4


def _r(v) -> float:
    return round(float(v), 12)


def _lesson(lv: ft.Level) -> dict:
    d = lv.lesson
    row = flow.RowLesson(d["air"], d["push"], d["tau"])
    steps = []
    for _ in range(d["steps"]):
        s = row.step()
        steps.append({k: [_r(v) for v in s[k]] for k in s})
    for _ in range(d["runSteps"]):
        row.step()
    return {"case": "flow_lesson", "tolerance": TOL, "steps": steps, "afterRun": [_r(v) for v in row.air]}


def _shape_case(name: str, shape: ft.Shape, lv: ft.Level, rid: str) -> dict:
    res = lv.resolution(rid)
    r = ft.simulate(shape, lv, res)
    rho, ux, uy = r.lbm.fields()
    nx, ny = res.size(lv)
    stride = 1 if nx <= 80 else 4
    cells = [y * nx + x for y in range(0, ny, stride) for x in range(0, nx, stride)]
    seats = ft.seats(shape, lv)
    time = ft.travel_time(r.drag, lv)
    total, avg, ramp = ft.steps(lv, res)
    return {
        "case": f"flow_{name}_{rid}", "tolerance": TOL, "resolution": rid,
        "nose": [list(p) for p in shape.points], "covered": shape.covered,
        "nx": nx, "ny": ny, "tau": _r(ft.tau(lv, res)), "steps": total, "averageSteps": avg, "ramp": ramp,
        "mask": ft.mask_rows(ft.mask(shape, lv, res)),
        "drag": _r(r.drag),
        "forceEvery50": [_r(v) for v in r.history[49::50]],
        "sampleCells": cells,
        "rho": [_r(rho[c]) for c in cells], "ux": [_r(ux[c]) for c in cells], "uy": [_r(uy[c]) for c in cells],
        "seats": seats, "speed": _r(ft.speed(r.drag, lv)), "time": _r(time), "stars": ft.stars(time, seats, lv),
    }


def generate(golden_dir: Path, data_dir: Path) -> None:
    lv = ft.load(data_dir / "Levels/MG2_PolarExpress.json")
    out = Path(golden_dir) / "flow"
    out.mkdir(parents=True, exist_ok=True)

    def write(doc):
        (out / f"{doc['case']}.json").write_text(json.dumps(doc, indent=1), encoding="utf-8")

    doc = _lesson(lv)
    write(doc)
    print("flow_lesson: nach dem Lauf", [round(v, 3) for v in doc["afterRun"]])

    cases = [("kalle", r) for r in ("grob", "mittel", "fein")] + [("start", r) for r in ("grob", "mittel", "fein")]
    cases += [("hint", "fein")]
    shapes = {k: ft.clamp_shape(ft.shape_from(v), lv) for k, v in lv.shapes.items()}
    # lange Spitznase: schnell, aber zu wenig Plätze (Zielkonflikt), mit einem offenen Übergang
    shapes["pointy"] = ft.clamp_shape(ft.Shape([(20, 2), (26, 3), (30, 12), (36, 12)], [True, False]), lv)
    cases += [("pointy", "fein")]
    for name, rid in cases:
        doc = _shape_case(name, shapes[name], lv, rid)
        write(doc)
        print(f"{doc['case']}: Beiwert {doc['drag']:.4f}, Plätze {doc['seats']}, Fahrzeit {doc['time']:.2f}, Sterne {doc['stars']}")

    rows = []
    for cd in (1.5, 2.0, 2.33, 2.6, 3.0, 3.6, 5.0):
        rows.append({"drag": cd, "speed": _r(ft.speed(cd, lv)), "time": _r(ft.travel_time(cd, lv))})
    write({"case": "flow_travel", "tolerance": 1e-9, "rows": rows})
