from pathlib import Path

import numpy as np
import pytest

from simref import settlement as s
from simref.cases import MELT_TWO_PASSES

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


def test_balance_closes_for_every_cell(level, mats):
    """Was reinkommt, geht auch raus – für jede einzelne Zelle, nicht nur in der Summe."""
    r = s.run(level.hint_tiles, level, mats)
    b = s.balance(r.final_tiles, level, mats, r.temperatures)
    np.testing.assert_allclose(b.source + b.from_neighbours, b.to_neighbours + b.to_air, rtol=1e-8, atol=1e-9)
    assert (b.from_neighbours >= 0).all() and (b.to_neighbours >= 0).all()


def test_balance_shows_lantern_feeding_its_neighbours(level, mats):
    g = [list(r) for r in level.initial_tiles]
    g[12][12] = "L"
    g[12][13] = "I"
    tiles = ["".join(r) for r in g]
    b = s.balance(tiles, level, mats, s.solve(tiles, level, mats))
    lantern, igloo = 12 * 24 + 12, 12 * 24 + 13
    assert b.from_neighbours[lantern] == 0.0          # nichts ist wärmer als die Laterne
    assert b.from_neighbours[igloo] > b.source[igloo]  # das Iglu lebt vor allem von der Laterne


@pytest.mark.parametrize("direction", list(s.WIND_DIRECTIONS))
def test_wall_shields_downwind_for_every_direction(level, mats, direction):
    """Der Windschatten liegt auf der Seite, wohin der Wind weht – in allen acht Richtungen."""
    g = [list(r) for r in level.initial_tiles]
    g[12][12] = "W"
    mask = s.shielded_mask(["".join(r) for r in g], mats, direction)
    dx, dy = s.WIND_DIRECTIONS[direction]
    lee = (12 + int(2 * dx), 12 + int(2 * dy))
    luv = (12 - int(2 * dx), 12 - int(2 * dy))
    assert mask[lee[1], lee[0]], f"{direction}: Lee {lee} nicht abgeschirmt"
    assert not mask[luv[1], luv[0]], f"{direction}: Luv {luv} abgeschirmt"


def test_melt_cascade_stops_at_the_pass_limit(level, mats):
    tiles = MELT_TWO_PASSES
    full = s.run(tiles, level, mats)
    capped = s.run(tiles, s.with_overrides(level, {"maxMeltPasses": 1}), mats)
    assert [len(p) for p in full.melt_passes] == [2, 1]
    assert [len(p) for p in capped.melt_passes] == [2]
    assert capped.final_tiles != full.final_tiles


def test_over_budget_earns_no_star_even_when_cosy(level, mats):
    g = [list(r) for r in level.hint_tiles]
    g[0][0] = "L"
    g[23][23] = "L"
    r = s.run(["".join(r) for r in g], level, mats)
    assert r.igloos_comfortable >= level.igloo_target and r.igloos_melted == 0
    assert r.cost > level.budget
    assert r.stars == 0


def test_model_without_wind_promises_cosy_igloos_that_really_freeze(level, mats):
    """„Was wäre, wenn wir den Wind weglassen?“ – die Vorhersage kippt von zu kalt zu gemütlich."""
    w = what_if(level)
    real = s.run(w["tiles"], level, mats)
    no_wind = s.run(w["tiles"], s.with_overrides(level, w["overrides"]), mats)
    assert real.igloos_total > 0 and real.igloos_cold == real.igloos_total
    assert no_wind.igloos_comfortable == real.igloos_total and no_wind.igloos_melted == 0


def test_what_if_settlement_is_not_the_example_solution(level):
    assert what_if(level)["tiles"] != level.hint_tiles


def what_if(level):
    import json
    d = json.loads((DATA / "Levels/MG1_Huddle_Settlement.json").read_text(encoding="utf-8"))
    return d["whatIf"]["noWind"]
