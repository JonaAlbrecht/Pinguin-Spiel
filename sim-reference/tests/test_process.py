"""MG3 „Fischfabrik“: die ereignisdiskrete Simulation (simref/process.py) und ihre Regeln."""
from dataclasses import replace
from pathlib import Path

import pytest
from scipy import stats

from simref import process as p
from simref import process_lesson as lesson_mod
from simref.process_random import Pcg32, exponential, triangular

DATA = Path(__file__).resolve().parents[2] / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
LEVEL = p.load_level(DATA / "Levels/MG3_Fischfabrik.json")
OLAF = LEVEL.raw["olafTiles"]


def cfg(rows=None, **changes):
    c = p.to_config(rows or OLAF, LEVEL)
    return replace(c, **changes) if changes else c


def in_transport(sim: p.FactorySimulation) -> int:
    pending = sum(1 for e in sim._heap if e[2] == p.ARRIVE)
    return pending + len(sim.transport_queue)


# ---------------------------------------------------------------- Zufall
def test_pcg32_matches_the_reference_implementation():
    """Erste Ausgaben von pcg32-demo (O'Neill) für Seed 42, Strom 54."""
    rng = Pcg32(42, 54)
    assert [rng.next_uint() for _ in range(6)] == [0xA15C02B7, 0x7B47F409, 0xBA1D3330, 0x83D2F293, 0xBFA4784B,
                                                   0xCBED606E]


def test_streams_are_independent_and_reproducible():
    a, b, c = Pcg32(7, 1), Pcg32(7, 1), Pcg32(7, 2)
    xs = [a.next_uint() for _ in range(20)]
    assert xs == [b.next_uint() for _ in range(20)]
    assert xs != [c.next_uint() for _ in range(20)]


def test_distributions_have_the_right_range_and_mean():
    rng = Pcg32(1, 3)
    exps = [exponential(rng, 20.0) for _ in range(20000)]
    assert min(exps) >= 0 and abs(sum(exps) / len(exps) - 20.0) < 0.6
    tris = [triangular(rng, 4.0, 6.0, 10.0) for _ in range(20000)]
    assert 4.0 <= min(tris) and max(tris) <= 10.0
    assert abs(sum(tris) / len(tris) - 20.0 / 3) < 0.05


def test_t_quantiles_match_scipy():
    for df in range(1, 31):
        assert p.t95(df) == pytest.approx(stats.t.ppf(0.95, df), abs=1e-6)
    assert p.t95(100) == pytest.approx(stats.norm.ppf(0.95), abs=1e-6)


# ---------------------------------------------------------------- Aufbau
def test_neighbours_hand_over_belts_carry_and_robots_walk():
    hs = p.hops(LEVEL.hint_tiles, LEVEL)
    assert [h.mode for h in hs] == [p.DIRECT, p.DIRECT, p.DIRECT, p.BELT_MODE]
    assert hs[0].time == LEVEL.handover
    assert hs[3].time == pytest.approx(LEVEL.belt_minutes_per_cell * 8)  # 7 Bandfelder + 1
    robot = p.hops(OLAF, LEVEL)
    assert all(h.mode == p.ROBOT_MODE for h in robot)
    assert robot[1].distance == 5 and robot[1].time == pytest.approx(LEVEL.robot_minutes_per_cell * 5)


def test_belt_takes_the_shortest_way_around_corners():
    rows = ["D..........T", "DSFP.......T", "D..B..#....T", "D..B.......T", "D..BBBBBBBBT", "D.K...#....T",
            "D..........T"]
    assert p.belt_length(rows, p.cells(rows, "P"), p.cells(rows, "T")) == 10


def test_belt_that_does_not_reach_is_no_belt():
    rows = ["D..........T", "DSFP.......T", "D..BBBB....T", "D..........T", "D.K...#....T", "D..........T",
            "D..........T"]
    assert p.belt_length(rows, p.cells(rows, "P"), p.cells(rows, "T")) is None


def test_missing_parts_are_reported():
    assert ("missing", "F") in p.problems(LEVEL.initial_tiles, LEVEL)
    no_robots = [r.replace("R", ".") for r in OLAF]
    assert ("noRobot", "0") in p.problems(no_robots, LEVEL)
    with pytest.raises(ValueError):
        p.to_config(no_robots, LEVEL)
    assert p.problems(LEVEL.hint_tiles, LEVEL) == []


def test_more_robots_than_parking_spots_slow_every_trip():
    assert cfg().robot_delay == 0
    kalle = p.to_config(LEVEL.kalle_tiles, LEVEL)
    assert kalle.robots == 6
    assert kalle.robot_delay == pytest.approx(LEVEL.crowd_delay * (6 - LEVEL.robot_parking))


def test_regal_capacity_and_cost_follow_the_tiles():
    c = p.to_config(LEVEL.hint_tiles, LEVEL)
    assert c.stages[1].capacity == 2 * LEVEL.regal_capacity
    assert c.stages[0].capacity == -1 and c.stages[2].capacity == -1
    assert p.cost(OLAF, LEVEL) == 300 + 400 + 300 + 100 + 3 * 100
    assert p.cost(LEVEL.hint_tiles, LEVEL) <= LEVEL.budget


# ---------------------------------------------------------------- Simulation
def test_same_seed_same_day_other_seed_other_day():
    a, b, c = (p.run_day(cfg(), s) for s in (5, 5, 6))
    assert a == b
    assert a != c


def test_no_box_vanishes():
    """Was ankommt, ist geklaut, verdorben, wartet, wird bearbeitet, ist unterwegs, steht bereit oder ist verladen."""
    for rows in (OLAF, LEVEL.kalle_tiles, LEVEL.hint_tiles):
        for seed in range(5):
            sim = p.FactorySimulation(cfg(rows), seed)
            for t in (37.0, 211.0, LEVEL.shift_length):
                sim.run_until(t)
                inside = sum(sim.in_station(s) for s in range(3)) + in_transport(sim) + sim.ready
                assert sim.result().arrived == sim.stolen + sim.spoiled + inside + sim.delivered


def test_full_regal_spoils_what_does_not_fit():
    full = cfg(stages=[replace(s, capacity=1 if s.capacity >= 0 else -1) for s in cfg().stages])
    roomy = cfg(stages=[replace(s, capacity=1000 if s.capacity >= 0 else -1) for s in cfg().stages])
    small, big = p.run_day(full, 1), p.run_day(roomy, 1)
    assert small.spoiled > 10 and big.spoiled == 0
    assert small.max_queue[1] <= 1


def test_gulls_steal_about_their_share():
    c = cfg(gull_chance=0.25)
    days = [p.run_day(c, s) for s in range(20)]
    share = sum(d.stolen for d in days) / sum(d.arrived for d in days)
    assert 0.2 < share < 0.3
    assert all(d.stolen == 0 for d in (p.run_day(cfg(gull_chance=0.0), s) for s in range(5)))


def test_breakdowns_pause_the_work_and_cost_output():
    calm = cfg(stages=[replace(s, mtbf=0.0) for s in cfg().stages])
    stormy = cfg(stages=[replace(s, mtbf=30.0, mttr=15.0) for s in cfg().stages])
    a = p.run_days(calm, 1, 10)
    b = p.run_days(stormy, 1, 10)
    assert b.mean_delivered < a.mean_delivered - 5


def test_interrupted_work_is_finished_after_the_repair():
    """Fällt eine Maschine mitten in der Arbeit aus, wird die Kiste nach der Reparatur fertig – nicht doppelt."""
    stormy = cfg(stages=[replace(s, mtbf=25.0, mttr=6.0) for s in cfg().stages])
    sim = p.FactorySimulation(stormy, 11, trace=True)
    sim.run_until(LEVEL.shift_length)
    done = [(t, a, b) for t, k, a, b in sim.trace if k == p.DONE]
    fails = [(t, a, b) for t, k, a, b in sim.trace if k == p.FAIL]
    assert len(fails) > 20
    # Jede fertige Kiste kommt genau einmal in der nächsten Stufe an: Fertig-Meldungen = Ankünfte dahinter
    arrivals_after = sum(1 for t, k, a, b in sim.trace if k == p.ARRIVE and a >= 1)
    assert len(done) >= arrivals_after
    # Während eine Maschine kaputt ist, meldet sie nichts fertig
    down = {}
    for t, k, a, b in sim.trace:
        if k == p.FAIL:
            down[(a, b)] = True
        elif k == p.REPAIR:
            down[(a, b)] = False
        elif k == p.DONE:
            assert not down.get((a, b), False)


def test_the_train_takes_what_is_ready():
    sim = p.FactorySimulation(cfg(), 2, trace=True)
    sim.run_until(LEVEL.shift_length)
    loaded = [a for t, k, a, b in sim.trace if k == p.TRAIN_ARRIVES]
    assert len(loaded) == int(LEVEL.shift_length // LEVEL.train_every)
    assert sum(loaded) == sim.delivered


def test_kutter_come_at_the_same_times_whatever_the_plan():
    """Gemeinsame Zufallszahlen: gleicher Tag, anderer Plan – nur der Plan erklärt den Unterschied."""
    def kutter_times(rows):
        sim = p.FactorySimulation(cfg(rows), 9, trace=True)
        sim.run_until(LEVEL.shift_length)
        return [t for t, k, a, b in sim.trace if k == p.KUTTER]
    assert kutter_times(OLAF) == kutter_times(LEVEL.hint_tiles) == kutter_times(LEVEL.kalle_tiles)


def test_littles_law_holds_for_one_station():
    """Analytische Probe (docs/04 4.4): Kisten im Mittel = Durchsatz · mittlere Verweilzeit (L = λ W)."""
    lesson = lesson_mod.load(DATA / "Levels/MG3_Fischfabrik.json")
    sim = p.FactorySimulation(lesson_mod.config(lesson), 123)
    area, last, end = 0.0, 0.0, 200000.0
    while sim.next_time <= end:
        n = sim.in_station(0)
        sim.step()
        area += n * (sim.time - last)
        last = sim.time
    area += sim.in_station(0) * (end - last)
    r = sim.result()
    assert area / end == pytest.approx(r.finished / end * r.lead_time, rel=0.02)


# ---------------------------------------------------------------- Das Level erzählt die Geschichte
def test_kalles_double_robots_make_it_worse():
    olaf = p.run_days(cfg(OLAF), LEVEL.seed, LEVEL.replications)
    kalle = p.run_days(cfg(LEVEL.kalle_tiles), LEVEL.seed, LEVEL.replications)
    assert kalle.mean_delivered < olaf.mean_delivered - 10
    assert kalle.mean_lead_time > olaf.mean_lead_time
    assert p.stars(olaf, LEVEL) == 0 and p.stars(kalle, LEVEL) == 0


def test_olafs_factory_stacks_up_in_front_of_the_filleting():
    day = p.run_day(cfg(OLAF), LEVEL.seed)
    assert day.bottleneck == 1
    assert day.spoiled > 10


def test_example_reaches_three_stars_within_budget():
    s = p.run_days(cfg(LEVEL.hint_tiles), LEVEL.seed, LEVEL.replications)
    assert p.stars(s, LEVEL) == 3
    assert p.cost(LEVEL.hint_tiles, LEVEL) <= LEVEL.budget


def test_build_budget_pays_for_a_running_factory():
    assert p.cost(OLAF, LEVEL) <= LEVEL.build_budget
    assert p.cost(LEVEL.kalle_tiles, LEVEL) > p.cost(OLAF, LEVEL)


def test_one_day_is_not_enough():
    """Die Möwen-Frage: Derselbe Plan ergibt an verschiedenen Tagen deutlich verschiedene Ergebnisse."""
    s = p.run_days(cfg(LEVEL.hint_tiles), LEVEL.seed, LEVEL.replications)
    assert s.max_delivered - s.min_delivered >= 15


def test_stars_follow_the_rules():
    days = [p.DayResult(80, 80, 90, 2, 0, 30.0, [0.5] * 3, [1] * 3, [0] * 3, 1, 100)] * 10
    s = p.summarize(days)
    assert s.sd_delivered == 0 and s.lower == 80
    assert p.stars(s, LEVEL) == 3
    assert p.stars(replace(s, mean_spoiled=LEVEL.spoil_max + 0.5), LEVEL) == 1
    assert p.stars(replace(s, lower=LEVEL.target - 1), LEVEL) == 2
    assert p.stars(replace(s, mean_delivered=LEVEL.target - 1), LEVEL) == 0
    assert p.lights(s, LEVEL) == (2, 2, 2)


def test_confidence_interval_uses_the_t_quantile():
    xs = [60, 70, 80, 65, 75, 72, 68, 77, 63, 70]
    days = [p.DayResult(x, x, 90, 0, 0, 1.0, [0.0] * 3, [0] * 3, [0] * 3, 0, 1) for x in xs]
    s = p.summarize(days)
    half = stats.t.ppf(0.95, 9) * stats.tstd(xs) / len(xs) ** 0.5
    assert s.half_width == pytest.approx(half, rel=1e-6)
