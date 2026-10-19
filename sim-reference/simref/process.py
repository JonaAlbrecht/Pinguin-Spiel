"""Python-Referenz für MG3 „Fischfabrik“: ereignisdiskrete Simulation einer Fischfabrik (docs/03, docs/04 4.3).

Ablauf einer Kiste:  Anlegestelle (D) → Sortieren (S) → Kühlregal → Filetieren (F) → Verpacken (P) → Verladung (T)

- Quellen: Kutter mit zufälligen Abständen (Exponentialverteilung), je Kutter mehrere Kisten; der Polar-Express
  im festen Takt bringt Nachschub-Kisten und nimmt alle fertigen Sushi-Kisten mit.
- Möwen: Jede ankommende Kiste wird mit kleiner Wahrscheinlichkeit geklaut.
- Stationen: Arbeitszeit dreieckverteilt, mehrere gleiche Maschinen arbeiten parallel. Störungen: jede Maschine fällt
  nach zufälliger Zeit aus (mittlere Zeit ``mtbf``) und wird zufällig lange repariert (``mttr``); eine angefangene
  Kiste wird danach fertig gemacht.
- Warteschlangen: vor dem Sortieren auf dem Steg (unbegrenzt), vor dem Filetieren im Kühlregal (begrenzt – was nicht
  mehr hineinpasst, verdirbt), vor dem Verpacken unbegrenzt.
- Wege: Stehen zwei Stationen direkt nebeneinander, wird übergeben. Verbindet ein Förderband sie, fährt die Kiste
  darauf. Sonst trägt eine Robo-Robbe sie (hin und zurück). Mehr Robo-Robben als Parkplätze stehen sich im Weg:
  jede Fahrt dauert dann länger (Vereinfachung der Wegreservierung, siehe Spec 005).

Ereignisse liegen in einem Heap, sortiert nach (Zeit, laufende Nummer) – gleiche Zeiten in Einfügereihenfolge.
Dieselbe Reihenfolge und dieselben Zufallsströme in C# (``SFE.SimCore.Process.FactorySimulation``).
Zeiten in Minuten ab Schichtbeginn.
"""
from __future__ import annotations

import heapq
import json
import math
from collections import deque
from dataclasses import dataclass, field
from pathlib import Path

from simref.process_random import (STREAM_FAILURES, STREAM_GULLS, STREAM_KUTTER, STREAM_SERVICE, Pcg32,
                                   exponential, triangular)

# ---------------------------------------------------------------- Raster-Zeichen
DOCK, TRAIN, FLOOR, WALL = "D", "T", ".", "#"
REGAL, BELT, ROBOT = "K", "B", "R"

# Ereignisarten (gleiche Zahlen in C#)
KUTTER, TRAIN_ARRIVES, DONE, ARRIVE, ROBOT_FREE, FAIL, REPAIR = range(7)

DIRECT, BELT_MODE, ROBOT_MODE = "direct", "belt", "robot"


# ---------------------------------------------------------------- Level
@dataclass
class Stage:
    id: str
    symbol: str
    low: float
    mode: float
    high: float
    mtbf: float
    mttr: float


@dataclass
class Level:
    width: int
    height: int
    initial_tiles: list[str]
    kalle_tiles: list[str]
    hint_tiles: list[str]
    shift_length: float
    kutter_mean_gap: float
    kutter_boxes: int
    kutter_last: float
    train_first: float
    train_every: float
    train_boxes: int
    gull_chance: float
    stages: list[Stage]
    regal_capacity: int
    robot_minutes_per_cell: float
    robot_parking: int
    crowd_delay: float
    belt_minutes_per_cell: float
    handover: float
    costs: dict[str, int]
    build_budget: int
    budget: int
    limits: dict[str, int]
    target: int
    spoil_max: float
    replications: int
    seed: int
    raw: dict = field(default_factory=dict, repr=False)


def load_level(path: Path) -> Level:
    d = json.loads(Path(path).read_text(encoding="utf-8"))
    return level_from_dict(d)


def level_from_dict(d: dict) -> Level:
    stages = [Stage(s["id"], s["symbol"], s["service"]["min"], s["service"]["mode"], s["service"]["max"],
                    s.get("mtbf", 0.0), s.get("mttr", 0.0)) for s in d["stages"]]
    k, t, tr = d["kutter"], d["train"], d["transport"]
    return Level(
        width=d["grid"]["width"], height=d["grid"]["height"],
        initial_tiles=d["initialTiles"], kalle_tiles=d["kalleTiles"], hint_tiles=d["hintTiles"],
        shift_length=d["shift"]["length"],
        kutter_mean_gap=k["meanGap"], kutter_boxes=k["boxes"], kutter_last=k["lastArrival"],
        train_first=t["first"], train_every=t["every"], train_boxes=t["boxes"],
        gull_chance=d["gulls"]["chance"],
        stages=stages, regal_capacity=d["regal"]["capacity"],
        robot_minutes_per_cell=tr["robotMinutesPerCell"], robot_parking=tr["robotParking"],
        crowd_delay=tr["crowdDelay"], belt_minutes_per_cell=tr["beltMinutesPerCell"], handover=tr["handover"],
        costs=dict(d["costs"]), build_budget=d["buildBudget"], budget=d["budget"], limits=dict(d["limits"]),
        target=d["goals"]["delivered"], spoil_max=d["goals"]["spoiledMax"],
        replications=d["replications"], seed=d["seed"], raw=d,
    )


# ---------------------------------------------------------------- Aufbau (Raster → Zahlen)
@dataclass
class Hop:
    """Weg zwischen zwei Stationen: Übergabe, Förderband oder Robo-Robbe; ``time`` = eine Richtung."""
    mode: str
    time: float
    distance: int


@dataclass
class StageConfig:
    machines: int
    low: float
    mode: float
    high: float
    mtbf: float
    mttr: float
    capacity: int  # Plätze in der Warteschlange davor, -1 = unbegrenzt


@dataclass
class FactoryConfig:
    shift_length: float
    kutter_mean_gap: float
    kutter_boxes: int
    kutter_last: float
    train_first: float
    train_every: float  # 0 = kein Zug
    train_boxes: int
    gull_chance: float
    stages: list[StageConfig]
    hops: list[Hop]     # len(stages) + 1: in Stufe 0, …, aus der letzten Stufe zur Verladung
    robots: int
    robot_delay: float  # Zusatzzeit je Fahrt, weil sich Robo-Robben im Weg stehen


def cells(rows: list[str], symbol: str) -> list[tuple[int, int]]:
    return [(x, y) for y, r in enumerate(rows) for x, c in enumerate(r) if c == symbol]


def count(rows: list[str], symbol: str) -> int:
    return sum(r.count(symbol) for r in rows)


def distance(a: list[tuple[int, int]], b: list[tuple[int, int]]) -> int:
    """Kürzester Abstand (Kästchen waagerecht + senkrecht) zwischen zwei Gruppen von Feldern."""
    return min(abs(ax - bx) + abs(ay - by) for ax, ay in a for bx, by in b)


def belt_length(rows: list[str], a: list[tuple[int, int]], b: list[tuple[int, int]]) -> int | None:
    """Anzahl Förderband-Felder auf dem kürzesten Band von Gruppe a nach Gruppe b; None = kein Band dazwischen.

    Breitensuche über Bandfelder: Start sind Bandfelder direkt neben a, Ziel ist ein Bandfeld direkt neben b."""
    h, w = len(rows), len(rows[0])
    near_a = {(x + dx, y + dy) for x, y in a for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))}
    near_b = {(x + dx, y + dy) for x, y in b for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1))}
    frontier = [(x, y) for y in range(h) for x in range(w) if rows[y][x] == BELT and (x, y) in near_a]
    seen = set(frontier)
    depth = 1
    while frontier:
        if any(c in near_b for c in frontier):
            return depth
        nxt = []
        for x, y in frontier:
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                nx, ny = x + dx, y + dy
                if 0 <= nx < w and 0 <= ny < h and rows[ny][nx] == BELT and (nx, ny) not in seen:
                    seen.add((nx, ny))
                    nxt.append((nx, ny))
        frontier = nxt
        depth += 1
    return None


def group_symbols(level: Level) -> list[str]:
    return [DOCK] + [s.symbol for s in level.stages] + [TRAIN]


def hops(rows: list[str], level: Level) -> list[Hop | None]:
    """Je Weg (Anlegestelle → erste Station, …, letzte Station → Verladung) die Art und Dauer; None = Station fehlt."""
    groups = [cells(rows, s) for s in group_symbols(level)]
    result: list[Hop | None] = []
    for a, b in zip(groups, groups[1:]):
        if not a or not b:
            result.append(None)
            continue
        dist = distance(a, b)
        if dist <= 1:
            result.append(Hop(DIRECT, level.handover, dist))
            continue
        belt = belt_length(rows, a, b)
        if belt is not None:
            result.append(Hop(BELT_MODE, level.belt_minutes_per_cell * (belt + 1), dist))
        else:
            result.append(Hop(ROBOT_MODE, level.robot_minutes_per_cell * dist, dist))
    return result


def problems(rows: list[str], level: Level) -> list[tuple[str, str]]:
    """Was dem Aufbau fehlt, damit Fisch durchläuft (für die Hinweise im Spiel). Leer = Fabrik läuft."""
    found = []
    for s in level.stages:
        if count(rows, s.symbol) == 0:
            found.append(("missing", s.symbol))
    if count(rows, REGAL) == 0:
        found.append(("missing", REGAL))
    if count(rows, ROBOT) == 0:
        for i, h in enumerate(hops(rows, level)):
            if h is not None and h.mode == ROBOT_MODE:
                found.append(("noRobot", str(i)))
    return found


def cost(rows: list[str], level: Level) -> int:
    """Kosten der gesetzten Bausteine (Anlegestelle, Verladung, Wände gehören zur Halle und kosten nichts)."""
    return sum(level.costs.get(c, 0) for r in rows for c in r)


def to_config(rows: list[str], level: Level) -> FactoryConfig:
    found = problems(rows, level)
    if found:
        raise ValueError("Aufbau unvollständig: " + str(found))
    hs = hops(rows, level)
    robots = count(rows, ROBOT)
    stages = []
    for i, s in enumerate(level.stages):
        # Das Kühlregal steht vor dem Filetieren (Stufe 1), dort ist die Warteschlange begrenzt
        cap = level.regal_capacity * count(rows, REGAL) if i == 1 else -1
        stages.append(StageConfig(count(rows, s.symbol), s.low, s.mode, s.high, s.mtbf, s.mttr, cap))
    return FactoryConfig(
        shift_length=level.shift_length, kutter_mean_gap=level.kutter_mean_gap, kutter_boxes=level.kutter_boxes,
        kutter_last=level.kutter_last, train_first=level.train_first, train_every=level.train_every,
        train_boxes=level.train_boxes, gull_chance=level.gull_chance, stages=stages, hops=hs, robots=robots,
        robot_delay=level.crowd_delay * max(0, robots - level.robot_parking),
    )


# ---------------------------------------------------------------- Simulation
@dataclass
class Machine:
    job: int = -1
    down: bool = False
    done: float = 0.0
    remaining: float = 0.0
    busy_since: float = 0.0
    version: int = 0


@dataclass
class DayResult:
    delivered: int
    finished: int
    arrived: int
    stolen: int
    spoiled: int
    lead_time: float
    utilization: list[float]
    max_queue: list[int]
    end_queue: list[int]
    bottleneck: int
    events: int


class FactorySimulation:
    def __init__(self, config: FactoryConfig, seed: int, trace: bool = False):
        self.c = config
        self.time = 0.0
        self._seq = 0
        self._heap: list = []
        self.trace: list[tuple] | None = [] if trace else None
        self.events = 0
        self.rng_kutter = Pcg32(seed, STREAM_KUTTER)
        self.rng_gulls = Pcg32(seed, STREAM_GULLS)
        self.rng_service = [Pcg32(seed, STREAM_SERVICE + s) for s in range(len(config.stages))]
        self.rng_fail = [[Pcg32(seed, STREAM_FAILURES + 10 * s + m) for m in range(st.machines)]
                         for s, st in enumerate(config.stages)]
        self.queues = [deque() for _ in config.stages]
        self.machines = [[Machine() for _ in range(st.machines)] for st in config.stages]
        self.busy = [0.0] * len(config.stages)
        self.max_queue = [0] * len(config.stages)
        self.arrival: list[float] = []
        self.robots_free = config.robots
        self.transport_queue: deque = deque()
        self.ready = 0
        self.delivered = self.finished = self.stolen = self.spoiled = 0
        self.lead_sum = 0.0

        if config.kutter_boxes > 0:
            first = exponential(self.rng_kutter, config.kutter_mean_gap)
            if first <= config.kutter_last:
                self._schedule(first, KUTTER)
        if config.train_every > 0 and config.train_first <= config.shift_length:
            self._schedule(config.train_first, TRAIN_ARRIVES)
        for s, st in enumerate(config.stages):
            if st.mtbf > 0:
                for m in range(st.machines):
                    t = exponential(self.rng_fail[s][m], st.mtbf)
                    if t <= config.shift_length:
                        self._schedule(t, FAIL, s, m)

    # ---- Ereignisliste
    def _schedule(self, t: float, kind: int, a: int = -1, b: int = -1, version: int = 0) -> None:
        heapq.heappush(self._heap, (t, self._seq, kind, a, b, version))
        self._seq += 1

    @property
    def next_time(self) -> float:
        return self._heap[0][0] if self._heap else math.inf

    def run_until(self, t_end: float) -> None:
        while self._heap and self._heap[0][0] <= t_end:
            self.step()
        self.time = max(self.time, t_end)

    def run_day(self) -> DayResult:
        self.run_until(self.c.shift_length)
        return self.result()

    def step(self) -> tuple | None:
        """Ein Ereignis abarbeiten (veraltete Fertig-Meldungen nach einer Störung zählen nicht). Gibt es zurück."""
        while self._heap:
            t, _, kind, a, b, version = heapq.heappop(self._heap)
            if kind == DONE and self.machines[a][b].version != version:
                continue  # Die Maschine fiel aus, während sie arbeitete – nach der Reparatur kommt eine neue Meldung
            self.time = t
            self.events += 1
            rec = self._handle(kind, a, b)
            if self.trace is not None:
                self.trace.append(rec)
            return rec
        return None

    def _handle(self, kind: int, a: int, b: int) -> tuple:
        now = self.time
        if kind == KUTTER:
            for _ in range(self.c.kutter_boxes):
                self._arrive_box(now)
            nxt = now + exponential(self.rng_kutter, self.c.kutter_mean_gap)
            if nxt <= self.c.kutter_last:
                self._schedule(nxt, KUTTER)
            return (now, kind, self.c.kutter_boxes, -1)
        if kind == TRAIN_ARRIVES:
            loaded = self.ready
            self.delivered += loaded
            self.ready = 0
            for _ in range(self.c.train_boxes):
                self._arrive_box(now)
            nxt = now + self.c.train_every
            if nxt <= self.c.shift_length:
                self._schedule(nxt, TRAIN_ARRIVES)
            return (now, kind, loaded, -1)
        if kind == DONE:
            m = self.machines[a][b]
            job = m.job
            m.job = -1
            self.busy[a] += now - m.busy_since
            self._send(a + 1, job, now)
            self._try_start(a, now)
            return (now, kind, a, b)
        if kind == ARRIVE:
            self._deliver(a, b, now)
            return (now, kind, a, b)
        if kind == ROBOT_FREE:
            self.robots_free += 1
            if self.transport_queue:
                h, job = self.transport_queue.popleft()
                self._start_trip(h, job, now)
            return (now, kind, -1, -1)
        if kind == FAIL:
            m = self.machines[a][b]
            m.down = True
            if m.job >= 0:
                m.remaining = m.done - now
                self.busy[a] += now - m.busy_since
                m.version += 1
            self._schedule(now + exponential(self.rng_fail[a][b], self.c.stages[a].mttr), REPAIR, a, b)
            return (now, kind, a, b)
        if kind == REPAIR:
            m = self.machines[a][b]
            m.down = False
            if m.job >= 0:
                m.done = now + m.remaining
                m.busy_since = now
                self._schedule_done(a, b, m.done)
            nxt = now + exponential(self.rng_fail[a][b], self.c.stages[a].mtbf)
            if nxt <= self.c.shift_length:
                self._schedule(nxt, FAIL, a, b)
            self._try_start(a, now)
            return (now, kind, a, b)
        raise ValueError(kind)

    def _schedule_done(self, s: int, m: int, t: float) -> None:
        self._schedule(t, DONE, s, m, self.machines[s][m].version)

    # ---- Kisten
    def _arrive_box(self, now: float) -> None:
        job = len(self.arrival)
        self.arrival.append(now)
        if self.c.gull_chance > 0 and self.rng_gulls.next_double() < self.c.gull_chance:
            self.stolen += 1
            return
        self._send(0, job, now)

    def _send(self, h: int, job: int, now: float) -> None:
        hop = self.c.hops[h]
        if hop.time <= 0:
            self._deliver(h, job, now)
        elif hop.mode != ROBOT_MODE:
            self._schedule(now + hop.time, ARRIVE, h, job)
        elif self.robots_free > 0:
            self._start_trip(h, job, now)
        else:
            self.transport_queue.append((h, job))

    def _start_trip(self, h: int, job: int, now: float) -> None:
        self.robots_free -= 1
        one_way = self.c.hops[h].time
        self._schedule(now + one_way + self.c.robot_delay, ARRIVE, h, job)
        self._schedule(now + 2 * one_way + self.c.robot_delay, ROBOT_FREE)

    def _deliver(self, h: int, job: int, now: float) -> None:
        if h < len(self.c.stages):
            self._enter_queue(h, job, now)
        else:
            self.finished += 1
            self.lead_sum += now - self.arrival[job]
            self.ready += 1

    def _enter_queue(self, s: int, job: int, now: float) -> None:
        cap = self.c.stages[s].capacity
        q = self.queues[s]
        if 0 <= cap <= len(q):
            self.spoiled += 1
            return
        q.append(job)
        self.max_queue[s] = max(self.max_queue[s], len(q))
        self._try_start(s, now)

    def _try_start(self, s: int, now: float) -> None:
        q = self.queues[s]
        st = self.c.stages[s]
        for i, m in enumerate(self.machines[s]):
            if not q:
                return
            if m.job >= 0 or m.down:
                continue
            m.job = q.popleft()
            m.done = now + triangular(self.rng_service[s], st.low, st.mode, st.high)
            m.busy_since = now
            self._schedule_done(s, i, m.done)

    # ---- Zustand für die Anzeige
    def in_station(self, s: int) -> int:
        """Kisten an einer Station: wartend plus in Arbeit."""
        return len(self.queues[s]) + sum(1 for m in self.machines[s] if m.job >= 0)

    def result(self) -> DayResult:
        end = self.time
        busy = list(self.busy)
        for s, ms in enumerate(self.machines):
            for m in ms:
                if m.job >= 0 and not m.down:
                    busy[s] += end - m.busy_since
        util = [busy[s] / (st.machines * end) if st.machines and end > 0 else 0.0
                for s, st in enumerate(self.c.stages)]
        bottleneck = max(range(len(util)), key=lambda s: (util[s], -s)) if util else -1
        return DayResult(
            delivered=self.delivered, finished=self.finished, arrived=len(self.arrival), stolen=self.stolen,
            spoiled=self.spoiled, lead_time=self.lead_sum / self.finished if self.finished else 0.0,
            utilization=util, max_queue=list(self.max_queue), end_queue=[len(q) for q in self.queues],
            bottleneck=bottleneck, events=self.events,
        )


def run_day(config: FactoryConfig, seed: int) -> DayResult:
    return FactorySimulation(config, seed).run_day()


# ---------------------------------------------------------------- Viele Tage
# t-Quantile (95 %, zweiseitig 90 %) für 1…30 Freiheitsgrade – fest eingetragen, damit C# ohne Statistikpaket
# dasselbe rechnet. Geprüft gegen scipy.stats.t.ppf in tests/test_process.py.
T95 = [6.313752, 2.919986, 2.353363, 2.131847, 2.015048, 1.943180, 1.894579, 1.859548, 1.833113, 1.812461,
       1.795885, 1.782288, 1.770933, 1.761310, 1.753050, 1.745884, 1.739607, 1.734064, 1.729133, 1.724718,
       1.720743, 1.717144, 1.713872, 1.710882, 1.708141, 1.705618, 1.703288, 1.701131, 1.699127, 1.697261]
Z95 = 1.644854


def t95(df: int) -> float:
    return T95[df - 1] if 1 <= df <= len(T95) else Z95


@dataclass
class Summary:
    days: list[DayResult]
    mean_delivered: float
    sd_delivered: float
    half_width: float      # halbe Breite des 90-%-Vertrauensbereichs des Mittelwerts
    lower: float           # Untergrenze – „auch an schlechten Tagen“
    mean_spoiled: float
    mean_stolen: float
    mean_lead_time: float
    min_delivered: int
    max_delivered: int


def day_seed(base: int, day: int) -> int:
    return base + day


def run_days(config: FactoryConfig, base_seed: int, n: int) -> Summary:
    days = [run_day(config, day_seed(base_seed, d)) for d in range(n)]
    return summarize(days)


def summarize(days: list[DayResult]) -> Summary:
    n = len(days)
    xs = [d.delivered for d in days]
    mean = sum(xs) / n
    sd = math.sqrt(sum((x - mean) ** 2 for x in xs) / (n - 1)) if n > 1 else 0.0
    half = t95(n - 1) * sd / math.sqrt(n) if n > 1 else 0.0
    return Summary(days, mean, sd, half, mean - half,
                   sum(d.spoiled for d in days) / n, sum(d.stolen for d in days) / n,
                   sum(d.lead_time for d in days) / n, min(xs), max(xs))


def stars(summary: Summary, level: Level) -> int:
    """★ im Schnitt pünktlich · ★★ dazu kaum Verdorbenes · ★★★ dazu auch an schlechten Tagen (Untergrenze ≥ Ziel)."""
    if summary.mean_delivered < level.target:
        return 0
    if summary.mean_spoiled > level.spoil_max:
        return 1
    return 3 if summary.lower >= level.target else 2


def lights(summary: Summary, level: Level) -> tuple[int, int, int]:
    """Ampeln 2 = grün, 1 = gelb, 0 = rot: pünktlich, kaum verdorben, auch an schlechten Tagen."""
    t = level.target
    on_time = 2 if summary.mean_delivered >= t else 1 if summary.mean_delivered >= 0.85 * t else 0
    fresh = 2 if summary.mean_spoiled <= level.spoil_max else 1 if summary.mean_spoiled <= 2 * level.spoil_max else 0
    robust = 2 if summary.lower >= t else 1 if summary.lower >= 0.85 * t else 0
    return on_time, fresh, robust
