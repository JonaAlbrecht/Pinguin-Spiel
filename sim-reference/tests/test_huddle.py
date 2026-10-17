import math
from pathlib import Path

import numpy as np

from simref import huddle as h

DATA = Path(__file__).resolve().parents[2] / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
P = h.load_params(DATA / "Levels/MG1A_LastHuddle.json")


def steady(pos, wind, steps=1500):
    t = np.full(len(pos), 25.0)
    for _ in range(steps):
        t = h.step(t, pos, wind, P, 1.0)
    return t


def test_single_penguin_approaches_analytic_steady_state():
    t = steady(np.array([[0.0, 0.0]]), (1.0, 0.0), steps=4000)
    assert math.isclose(t[0], P.air_temp + P.heat / P.h_exposed, abs_tol=1e-6)


def test_upwind_penguin_shields_downwind_one():
    pos = np.array([[0.0, 0.0], [0.75, 0.0]])
    exposed = h.exposure(pos, (1.0, 0.0), P)   # Wind weht nach +x: Pinguin 0 steht im Luv
    assert exposed[0] and not exposed[1]


def test_compact_huddle_keeps_everyone_warm_and_loner_freezes():
    pts = []
    for row in range(-3, 4):
        for col in range(-3, 4):
            pts.append(((col + 0.5 * (row % 2)) * 0.75, row * 0.75 * math.sqrt(3) / 2))
    pts.sort(key=lambda q: q[0] ** 2 + q[1] ** 2)
    t = steady(np.array(pts[:12]), (1.0, 0.0))
    assert t.min() > 18.0
    alone = steady(np.array([[0.0, 0.0]]), (1.0, 0.0))
    assert alone[0] < 18.0


PAIR = h.pair_lesson(DATA / "Levels/MG1A_LastHuddle.json")


def run_pair():
    pos, temps, wind, dt, steps = PAIR
    history = [temps]
    terms = []
    for _ in range(steps):
        after = h.step(history[-1], pos, wind, P, dt)
        terms.append(h.step_terms(history[-1], after, pos, wind, P, dt))
        history.append(after)
    return history, terms


def test_step_terms_add_up_exactly():
    """Die Bild-Gleichung im Prolog: vorher + selbst + vom Nachbarn - an den Wind ergibt neu – ohne Rest."""
    history, terms = run_pair()
    for before, after, t in zip(history, history[1:], terms):
        np.testing.assert_allclose(after - before, t.own + t.from_neighbours - t.to_wind, rtol=0, atol=1e-10)


def test_exchange_between_penguins_conserves_heat():
    """Was Pip bekommt, gibt Grete ab (Wärmeerhaltung im Austausch)."""
    _, terms = run_pair()
    for t in terms:
        assert abs(t.from_neighbours.sum()) < 1e-12
        assert t.from_neighbours[1] > 0 > t.from_neighbours[0]


def test_pair_lesson_shows_shelter_and_warming():
    history, terms = run_pair()
    grete, pip = 0, 1
    assert terms[0].to_wind[grete] > terms[0].to_wind[pip]          # Grete steht im Wind, Pip im Windschatten
    gaps = [t[grete] - t[pip] for t in history]
    assert all(a > b for a, b in zip(gaps, gaps[1:]))                 # der Unterschied wird mit jedem Schritt kleiner
    assert history[-1][pip] > history[0][pip]                         # Pip wird wärmer
    # Lesbare Zahlen im Spiel: jeder Teil zwischen 0,1 und 5 je Schritt
    first = terms[0]
    for part in (first.own, abs(first.from_neighbours), first.to_wind):
        assert ((part > 0.1) & (part < 5)).all()
