from dataclasses import replace
from pathlib import Path

import numpy as np

from simref import settlement as s
from simref import settlement_pair as sp

DATA = Path(__file__).resolve().parents[2] / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
LEVEL_PATH = DATA / "Levels/MG1_Huddle_Settlement.json"
P = sp.load(LEVEL_PATH)
LEVEL = s.load_level(LEVEL_PATH)
LANTERN, IGLOO = 0, 1


def run(steps=None):
    history = [sp.start(P)]
    terms = []
    for _ in range(P.steps if steps is None else steps):
        history.append(sp.step(history[-1], P))
        terms.append(sp.step_terms(history[-1], P))
    return history, terms


def test_step_terms_add_up_exactly():
    """Die Rechentafel: vorher + selbst + vom Nachbarn - an den Wind ergibt neu – ohne Rest."""
    history, terms = run()
    for before, after, t in zip(history, history[1:], terms):
        np.testing.assert_allclose(after - before, t.own + t.from_neighbour - t.to_wind, rtol=0, atol=1e-10)


def test_exchange_between_fields_conserves_heat():
    """Was das Iglu bekommt, gibt die Laterne ab."""
    _, terms = run()
    for t in terms:
        assert abs(t.from_neighbour.sum()) < 1e-12
        assert t.from_neighbour[IGLOO] > 0 > t.from_neighbour[LANTERN]


def test_steps_approach_the_steady_state_of_the_settlement():
    """Der Gleichgewichtspunkt der Tafel ist die stationäre Bilanz der Siedlung (gleiche Formel ohne Zeit)."""
    history, _ = run(steps=200)
    np.testing.assert_allclose(history[-1], sp.steady(P), atol=1e-9)
    # stationäre Bilanz je Feld geht auf: selbst + vom Nachbarn = an den Wind
    t = sp.steady(P)
    flow = P.k * (t[1 - np.arange(2)] - t)
    np.testing.assert_allclose(P.heat + flow, P.h * (t - P.air_temp), atol=1e-12)


def test_steady_state_is_what_the_settlement_solver_computes():
    """Keine zweite Wahrheit (ADR-12): Das Siedlungsmodell auf einem 2×1-Raster „L I“ mit den Werten der Tafel
    landet genau im Gleichgewichtspunkt der Tafel."""
    mats = s.MaterialSet("pair", shield_range=0.0, shield_cone=1.0, shield_factor=1.0, wind_reference=1.0, materials=[
        s.Material("lantern", "L", P.k, P.h, 0.0),
        s.Material("igloo", "I", P.k, P.h, P.igloo_heat),
    ])
    level = replace(LEVEL, wind=1.0, air_temp=P.air_temp, lantern_heat=P.lantern_heat)
    np.testing.assert_allclose(s.solve(["LI"], level, mats).ravel(), sp.steady(P), atol=1e-10)


def test_igloo_becomes_comfortable_and_changes_shrink():
    history, _ = run()
    igloo = [t[IGLOO] for t in history]
    assert igloo[0] == LEVEL.air_temp
    assert LEVEL.comfort_min <= igloo[-1] < LEVEL.melt                 # am Ende gemütlich, nichts schmilzt
    assert LEVEL.comfort_min <= sp.steady(P)[IGLOO] < LEVEL.melt         # auch der Gleichgewichtspunkt
    changes = [b - a for a, b in zip(igloo, igloo[1:])]
    assert all(c > 0 for c in changes)
    assert all(a > b for a, b in zip(changes, changes[1:]))             # jeder Schritt ändert weniger


def test_numbers_are_readable_in_the_first_step():
    """Lesbare Zahlen im Spiel: jeder Teil zwischen 0,1 und 20 im ersten Schritt (außer der Eigenwärme des Iglus)."""
    _, terms = run()
    first = terms[0]
    assert 0.1 < first.own[LANTERN] < 50
    for part in (abs(first.from_neighbour), first.to_wind):
        assert ((part > 0.1) & (part < 20)).all()
