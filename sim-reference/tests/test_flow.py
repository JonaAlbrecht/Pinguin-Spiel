"""Lattice-Boltzmann-Kern (MG2 Polar-Express): Erhaltung, Gleichgewicht, Ränder, Rechentafel."""
from pathlib import Path

import numpy as np
import pytest

from simref import flow

DATA = Path(__file__).resolve().parents[2] / "unity/PinguinKolonie/Assets/_Project/Resources/Data"


def test_equilibrium_has_the_right_air_and_push():
    """Das Gleichgewicht trägt genau die Luftmenge und den Schwung, aus denen es gebaut ist."""
    feq = flow.equilibrium([1.3], [0.07], [-0.02])
    rho, ux, uy = flow.moments(feq)
    assert rho[0] == pytest.approx(1.3, abs=1e-14)
    assert ux[0] == pytest.approx(0.07, abs=1e-14)
    assert uy[0] == pytest.approx(-0.02, abs=1e-14)


def test_tables_are_consistent():
    assert all(flow.OPP[flow.OPP[k]] == k for k in range(9))
    for k in range(9):
        assert flow.C[flow.OPP[k]] == (-flow.C[k][0], -flow.C[k][1])
        assert flow.C[flow.MIRROR_Y[k]] == (flow.C[k][0], -flow.C[k][1])
    assert sum(flow.W) == pytest.approx(1.0, abs=1e-15)


def test_closed_box_keeps_its_air_and_settles():
    """Im geschlossenen Kasten geht keine Luft verloren, und ein Windstoß beruhigt sich."""
    rng = np.random.default_rng(3)
    nx, ny = 12, 7
    solid = np.zeros(nx * ny, bool)
    solid[3 * nx + 5] = solid[3 * nx + 6] = True
    lbm = flow.Lbm(nx, ny, 0.8, solid)
    rho = 1.0 + 0.2 * rng.random(nx * ny)
    lbm.f = flow.equilibrium(rho, 0.05 * rng.random(nx * ny), np.zeros(nx * ny))
    lbm.f[:, solid] = flow.equilibrium(np.ones(2), np.zeros(2), np.zeros(2))
    air0 = flow.moments(lbm.f)[0][~solid].sum()
    for _ in range(400):
        lbm.step()
        assert flow.moments(lbm.f)[0][~solid].sum() == pytest.approx(air0, rel=1e-12)
    _, ux, uy = lbm.fields()
    assert np.abs(ux).max() < 1e-3 and np.abs(uy).max() < 1e-3


def test_collision_keeps_air_and_push_in_every_cell():
    rng = np.random.default_rng(1)
    lbm = flow.Lbm(6, 4, 0.7)
    lbm.f = flow.equilibrium(np.ones(24), np.zeros(24), np.zeros(24)) + 0.01 * rng.random((9, 24))
    post = lbm.collide(lbm.f)
    for a, b in zip(flow.moments(lbm.f), flow.moments(post)):
        np.testing.assert_allclose(a, b, atol=1e-14)


def test_empty_channel_carries_the_inflow_without_drag():
    """Ohne Zug strömt die Luft gleichmäßig durch (glatte Ränder), und niemand bremst."""
    lbm = flow.Lbm(40, 10, 0.8, u_in=0.08, ramp=50)
    lbm.step(600)
    rho, ux, uy = lbm.fields()
    np.testing.assert_allclose(ux, 0.08, atol=2e-3)
    np.testing.assert_allclose(uy, 0.0, atol=2e-3)
    assert lbm.links.size == 0 and lbm.force == (0.0, 0.0)


def test_inflow_ramps_up_smoothly():
    assert flow.inflow_speed(0.1, 100, 0) == 0.0
    assert flow.inflow_speed(0.1, 100, 50) == pytest.approx(0.05)
    assert flow.inflow_speed(0.1, 100, 100) == 0.1
    assert flow.inflow_speed(0.1, 0, 0) == 0.1


def test_bounce_back_reverses_a_population():
    """Ein Anteil, der auf den Zug zuläuft, kommt im nächsten Schritt in Gegenrichtung zurück."""
    solid = np.zeros(3, bool)
    solid[2] = True
    lbm = flow.Lbm(3, 1, 1e9, solid)  # tau riesig: praktisch kein Ausgleichen; Grundzustand ruhende Luft
    lbm.f[1, 1] += 0.5  # zusätzlich nach rechts, direkt vor dem Zug
    lbm.step()
    assert lbm.f[3, 1] == pytest.approx(1 / 9 + 0.5, rel=1e-6)
    # doppelter Schwung: gerade (1/9 + 0,5) und die zwei schrägen Anteile (je 1/36), die oben/unten gespiegelt anprallen
    assert lbm.force[0] == pytest.approx(2 * (1 / 9 + 0.5) + 4 / 36, rel=1e-6)


@pytest.fixture(scope="module")
def lesson():
    import json
    d = json.loads((DATA / "Levels/MG2_PolarExpress.json").read_text(encoding="utf-8"))["lesson"]
    row = flow.RowLesson(d["air"], d["push"], d["tau"])
    return d, row, [row.step() for _ in range(d["steps"])]


def test_lesson_table_adds_up_exactly(lesson):
    """vorher + kommt an - zieht weiter = neu, für jedes Kästchen und jeden Schritt."""
    _, _, steps = lesson
    for s in steps:
        np.testing.assert_allclose(s["before"] + s["comeIn"] - s["moveOn"], s["after"], atol=1e-13)


def test_lesson_air_is_never_lost(lesson):
    """Was ein Kästchen abgibt, bekommt ein anderes: „Luft geht nicht verloren“."""
    d, _, steps = lesson
    total = sum(d["air"])
    for s in steps:
        assert s["comeIn"].sum() == pytest.approx(s["moveOn"].sum(), abs=1e-13)
        assert s["after"].sum() == pytest.approx(total, abs=1e-12)


def test_evening_out_keeps_the_air_of_each_cell(lesson):
    _, _, steps = lesson
    for s in steps:
        even = s["rightEven"] + s["leftEven"] + s["stayEven"]
        np.testing.assert_allclose(even, s["before"], atol=1e-14)


def test_lesson_gust_travels_right_and_evens_out(lesson):
    d, row, steps = lesson
    first = steps[0]
    assert first["after"][1] > first["before"][1]          # der Stoß kommt beim Nachbarn an
    for _ in range(d["runSteps"]):
        row.step()
    air = row.air
    assert air.max() - air.min() < 0.02 * air.mean()         # am Ende fast gleich verteilt
