"""Rechentafel in MG3 Schritt 2: eine einzige Filetier-Station mit Warteschlange, Ereignis für Ereignis.

Kein eigenes Modell – dieselbe Simulation wie die ganze Fabrik (``process.FactorySimulation``), nur mit einer Station,
einzeln ankommenden Kisten, ohne Zug, Möwen, Störungen und Wege. So sind die Zahlen auf der Tafel genau die, die der
Computer später für alle Stationen zugleich rechnet:

    Kisten vorher + kommen an - werden fertig = Kisten neu

Jede Zeile ist ein Ereignis: Entweder kommt eine Kiste an (+1) oder die Station wird mit einer fertig (-1).
Dazwischen ändert sich nichts – darum springt der Computer von Ereignis zu Ereignis.
"""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from simref import process as p


@dataclass
class Lesson:
    seed: int
    arrival_mean: float
    low: float
    mode: float
    high: float
    events: int


@dataclass
class Row:
    time: float
    kind: str        # "arrive" oder "done"
    before: int
    arrived: int
    finished: int
    after: int
    waiting: int     # davon in der Warteschlange
    working: int     # davon in Arbeit (0 oder 1)


def load(path: Path) -> Lesson:
    d = json.loads(Path(path).read_text(encoding="utf-8"))["stationLesson"]
    s = d["service"]
    return Lesson(d["seed"], d["arrivalMean"], s["min"], s["mode"], s["max"], d["events"])


def config(lesson: Lesson) -> p.FactoryConfig:
    endless = 1e9
    return p.FactoryConfig(
        shift_length=endless, kutter_mean_gap=lesson.arrival_mean, kutter_boxes=1, kutter_last=endless,
        train_first=0.0, train_every=0.0, train_boxes=0, gull_chance=0.0,
        stages=[p.StageConfig(1, lesson.low, lesson.mode, lesson.high, 0.0, 0.0, -1)],
        hops=[p.Hop(p.DIRECT, 0.0, 0), p.Hop(p.DIRECT, 0.0, 0)], robots=0, robot_delay=0.0)


def run(lesson: Lesson, events: int | None = None) -> list[Row]:
    sim = p.FactorySimulation(config(lesson), lesson.seed)
    rows = []
    for _ in range(lesson.events if events is None else events):
        before = sim.in_station(0)
        t, kind, _, _ = sim.step()
        after = sim.in_station(0)
        arrived = 1 if kind == p.KUTTER else 0
        finished = 1 if kind == p.DONE else 0
        rows.append(Row(t, "arrive" if arrived else "done", before, arrived, finished, after,
                        len(sim.queues[0]), after - len(sim.queues[0])))
    return rows
