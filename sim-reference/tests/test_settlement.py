from pathlib import Path

import numpy as np
import pytest

from simref import settlement as s

DATA = Path(__file__).resolve().parents[2] / "unity/PinguinKolonie/Assets/_Project/Resources/Data"


@pytest.fixture(scope="module")
def mats():
    return s.load_materials(DATA / "Materials/materials_settlement_v1.json")


@pytest.fixture(scope="module")
def level():
    return s.load_level(DATA / "Levels/MG1_Huddle_Settlement.json")


def test_empty_field_is_at_air_temperature(level, mats):
    t = s.solve(level.initial_tiles, level, mats)
    assert np.allclose(t, level.air_temp)


def test_matrix_is_symmetric_positive_definite(level, mats):
    a, _ = s.assemble(level.hint_tiles, level, mats)
    dense = a.toarray()
    assert np.allclose(dense, dense.T)
    assert np.linalg.eigvalsh(dense).min() > 0


def test_energy_balance(level, mats):
    """Alle Wärmequellen = Verlust an die Luft (Leitung zwischen Zellen hebt sich auf)."""
    tiles = level.hint_tiles
    a, b = s.assemble(tiles, level, mats)
    t = s.solve(tiles, level, mats).ravel()
    sources = sum(level.lantern_heat if c == "L" else mats.by_symbol(c).q
                  for row in tiles for c in row)
    # Verlust = sum h_i (T_i - T_luft); h_i = Diagonale minus Summe der Kopplungen
    coupling = np.asarray(-a.sum(axis=1)).ravel() + a.diagonal()
    h = a.diagonal() - coupling
    assert np.isclose(sources, np.sum(h * (t - level.air_temp)), rtol=1e-8)


def test_lantern_warms_neighbours_more_than_far_cells(level, mats):
    g = [list(r) for r in level.initial_tiles]
    g[12][12] = "L"
    t = s.solve(["".join(r) for r in g], level, mats)
    assert t[12, 12] > t[12, 13] > t[12, 14] > t[12, 16] > level.air_temp


def test_wall_shields_downwind_only(level, mats):
    g = [list(r) for r in level.initial_tiles]
    g[10][10] = "W"
    mask = s.shielded_mask(["".join(r) for r in g], mats, "NW")
    assert mask[11, 11] and mask[12, 12]      # Lee (Südosten)
    assert not mask[9, 9]                      # Luv
    assert not mask[10, 10]                    # die Mauer selbst


def test_hint_layout_reaches_two_stars(level, mats):
    r = s.run(level.hint_tiles, level, mats)
    assert r.igloos_comfortable >= level.igloo_target
    assert r.igloos_melted == 0
    assert r.stars == 2


def test_kalle_layout_melts(level, mats):
    r = s.run(level.kalle_tiles, level, mats)
    assert r.igloos_melted >= 5
    assert r.stars == 0
