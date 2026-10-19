from pathlib import Path

import numpy as np

from simref import settlement as s
from simref import settlement_wave as sw

DATA = Path(__file__).resolve().parents[2] / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
LEVEL_PATH = DATA / "Levels/MG1_Huddle_Settlement.json"
LEVEL = s.load_level(LEVEL_PATH)
MATS = s.load_materials(DATA / "Materials/materials_settlement_v1.json")
WAVE = sw.load(LEVEL_PATH)


def igloos_by_distance():
    """(y, x) der Laterne und der Iglus, die Iglus nach Abstand zur Laterne sortiert."""
    lantern = next((y, x) for y, r in enumerate(WAVE.tiles) for x, c in enumerate(r) if c == "L")
    igloos = [(y, x) for y, r in enumerate(WAVE.tiles) for x, c in enumerate(r) if c == "I"]
    return lantern, sorted(igloos, key=lambda p: abs(p[0] - lantern[0]) + abs(p[1] - lantern[1]))


def test_layout_has_one_lantern_and_igloos_near_and_far():
    lantern, igloos = igloos_by_distance()
    distances = sorted({abs(y - lantern[0]) + abs(x - lantern[1]) for y, x in igloos})
    assert len(distances) >= 2 and distances[0] < distances[-1]
    assert sum(r.count("L") for r in WAVE.tiles) == 1


def test_wave_ends_at_the_settlement_steady_state():
    """Keine zweite Wahrheit: Nach allen Schritten ist die Welle (fast) beim Ergebnis des Siedlungs-Lösers."""
    history = sw.run(WAVE, LEVEL, MATS)
    np.testing.assert_allclose(history[-1], s.solve(WAVE.tiles, LEVEL, MATS), atol=0.1)
    long = sw.run(sw.WaveLesson(WAVE.capacity, WAVE.dt, 300, WAVE.manual_steps, WAVE.tiles), LEVEL, MATS)
    np.testing.assert_allclose(long[-1], s.solve(WAVE.tiles, LEVEL, MATS), atol=1e-8)


def test_heat_arrives_later_and_weaker_far_away():
    """Das nahe Iglu wird gleich wärmer, das ferne später und weniger – die Wärme wandert von Feld zu Feld."""
    history = sw.run(WAVE, LEVEL, MATS)
    _, igloos = igloos_by_distance()
    near, far = igloos[0], igloos[-1]
    first = history[0]
    assert first[near] - LEVEL.air_temp > 3 * (first[far] - LEVEL.air_temp)   # Schritt 1: nah viel mehr als fern
    for a, b in zip(history, history[1:]):                                     # alle Iglus werden nur wärmer
        assert all(b[p] >= a[p] - 1e-12 for p in igloos)
    assert history[-1][far] - LEVEL.air_temp > 1.0                            # am Ende kommt auch fern etwas an
    assert history[-1][near] > history[-1][far]                               # aber weniger als nah


def test_manual_steps_show_visible_changes():
    history = sw.run(WAVE, LEVEL, MATS)
    _, igloos = igloos_by_distance()
    near = igloos[0]
    temps = [LEVEL.air_temp] + [h[near] for h in history[:WAVE.manual_steps]]
    assert all(b - a > 0.5 for a, b in zip(temps, temps[1:]))                 # jeder Klick sichtbar (> 0,5 Grad)
    assert 0 < WAVE.manual_steps < WAVE.steps
