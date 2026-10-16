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
