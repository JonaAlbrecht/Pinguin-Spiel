"""MG2 Polar-Express: Zugform, Plätze, Fahrzeit, Sterne und das Leveldesign (Zielkonflikt Nase gegen Plätze).

Teure Strömungsrechnungen fein laufen nur in generate_golden.py (golden/flow) und in den C#-Tests; hier wird
grob gerechnet und gegen die Golden Files geprüft.
"""
import json
from pathlib import Path

import numpy as np
import pytest

from simref import flow_train as ft

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
GOLDEN = ROOT / "golden/flow"
LV = ft.load(DATA / "Levels/MG2_PolarExpress.json")
SHAPES = {k: ft.clamp_shape(ft.shape_from(v), LV) for k, v in LV.shapes.items()}


def golden(name):
    return json.loads((GOLDEN / f"{name}.json").read_text(encoding="utf-8"))


def test_profile_follows_the_handles():
    s = SHAPES["hint"]
    assert ft.profile(s, LV, LV.start) == pytest.approx(s.points[0][1], abs=1e-9)        # Stirn = P0
    assert ft.profile(s, LV, s.points[3][0] - 1e-9) == pytest.approx(LV.height, abs=1e-6)  # Dach ab P3
    mid = LV.start + LV.length / 2
    assert ft.profile(s, LV, mid) == LV.height
    # Heck ist das Spiegelbild der Nase
    for x in (20.5, 23.3, 27.9):
        assert ft.profile(s, LV, x) == pytest.approx(ft.profile(s, LV, 2 * LV.start + LV.length - x), abs=1e-9)
    assert ft.profile(s, LV, LV.start - 0.1) == 0.0 and ft.profile(s, LV, LV.start + LV.length) == 0.0


def test_nose_height_rises_monotonically_for_a_rounded_nose():
    p = SHAPES["hint"].points
    xs = np.linspace(p[0][0], p[3][0], 50)
    hs = [ft.nose_height(p, x) for x in xs]
    assert all(b >= a - 1e-12 for a, b in zip(hs, hs[1:]))


def test_clamp_keeps_handles_in_the_nose_area():
    wild = ft.Shape([(5, 40), (90, -3), (10, 30), (200, 1)], [False, True])
    s = ft.clamp_shape(wild, LV)
    xs = [p[0] for p in s.points]
    assert xs == sorted(xs)
    assert s.points[0][0] == LV.start and s.points[3][1] == LV.height
    assert s.points[3][0] <= LV.start + LV.nose_max
    assert all(0 <= p[1] <= LV.height for p in s.points)
    assert s.points[0][1] >= LV.tip_min


def test_open_transitions_stick_out_and_covered_ones_are_flush():
    open_, cov = SHAPES["start"], ft.Shape(SHAPES["start"].points, [True, True])
    x = LV.start + LV.transitions[0] + 1
    assert ft.profile(open_, LV, x) == LV.transition_height > LV.height
    assert ft.profile(cov, LV, x) == LV.height


def test_mask_is_the_same_rule_in_every_resolution():
    """Feiner rastern verändert die Form nicht, nur die Treppenstufen: gleiche Zugfläche ± eine Randreihe."""
    s = SHAPES["hint"]
    areas = []
    for r in LV.resolutions:
        m = ft.mask(s, LV, r)
        areas.append(m.sum() / r.scale ** 2)
        nx, _ = r.size(LV)
        assert not m[:, 0].any() and not m[:, -1].any() and not m[:, -2].any()  # Ein- und Ausströmen frei
    assert max(areas) / min(areas) < 1.1


def test_grob_cannot_tell_kalles_box_from_the_rounded_nose():
    """Lernmoment Schritt 3: grob ist die runde Nase eine Treppe – genau wie Kalles Kiste. Fein nicht."""
    grob, fein = LV.resolution("grob"), LV.resolution("fein")
    assert (ft.mask(SHAPES["kalle"], LV, grob) == ft.mask(SHAPES["start"], LV, grob)).all()
    assert (ft.mask(SHAPES["kalle"], LV, fein) != ft.mask(SHAPES["start"], LV, fein)).any()


def test_seats_drop_with_a_long_nose():
    box, hint = ft.seats(SHAPES["kalle"], LV), ft.seats(SHAPES["hint"], LV)
    pointy = ft.seats(ft.clamp_shape(ft.Shape([(20, 2), (26, 3), (30, 12), (36, 12)], [True, True]), LV), LV)
    assert box > hint >= LV.passengers > pointy
    assert box % LV.seats_per_row == 0


def test_travel_time_follows_the_force_balance():
    """F = R + C (v + w)² – nachgerechnet mit der gefundenen Geschwindigkeit."""
    for cd in (2.0, 2.5, 3.0):
        v = ft.speed(cd, LV)
        c = LV.drag_scale * cd
        assert LV.rolling + c * (v + LV.headwind) ** 2 == pytest.approx(LV.traction, rel=1e-12)
        assert ft.travel_time(cd, LV) == pytest.approx(LV.distance / v)
    assert ft.speed(50.0, LV) == LV.min_speed                     # gegen den Sturm: fast Stillstand


def test_stars_follow_the_rules():
    assert ft.stars(LV.target_time + 0.1, 999, LV) == 0
    assert ft.stars(LV.target_time, LV.passengers - 1, LV) == 1
    assert ft.stars(LV.target_time, LV.passengers, LV) == 2
    assert ft.stars(LV.best_time, LV.passengers, LV) == 3


def test_grob_run_matches_the_golden_file():
    """Kurzer Abgleich der Python-Referenz mit dem eigenen Golden File (grob ist schnell)."""
    g = golden("flow_start_grob")
    r = ft.simulate(SHAPES["start"], LV, LV.resolution("grob"))
    assert r.drag == pytest.approx(g["drag"], rel=1e-9)
    assert ft.mask_rows(ft.mask(SHAPES["start"], LV, LV.resolution("grob"))) == g["mask"]


# ---------- Leveldesign (aus den Golden Files, fein gerechnet) ----------

def test_kalle_nearly_stops_and_the_hint_reaches_three_stars():
    kalle, start, hint = golden("flow_kalle_fein"), golden("flow_start_fein"), golden("flow_hint_fein")
    assert kalle["stars"] == 0 and start["stars"] == 0
    assert hint["stars"] == 3
    assert kalle["time"] > 2.5 * hint["time"]                     # Kalles Kiste kommt kaum vom Fleck
    assert kalle["drag"] > start["drag"] > hint["drag"]


def test_a_very_pointy_nose_is_fast_but_lacks_seats():
    pointy, hint = golden("flow_pointy_fein"), golden("flow_hint_fein")
    assert pointy["time"] < LV.target_time and pointy["seats"] < LV.passengers
    assert pointy["stars"] == 1 and hint["seats"] >= LV.passengers


def test_finer_resolution_converges():
    """Die Auflösungen nähern sich an: grob weicht mehr ab als mittel (Schritt 3 „Wie genau?“)."""
    for name in ("kalle", "start"):
        g, m, f = (golden(f"flow_{name}_{r}")["drag"] for r in ("grob", "mittel", "fein"))
        assert abs(g - f) > abs(m - f)
        assert abs(g - f) / f < 0.1


def test_covered_transitions_lower_the_drag():
    """Verkleidete Übergänge: weniger Luftwiderstand, gleich viele Plätze (grob gerechnet)."""
    grob = LV.resolution("grob")
    open_ = SHAPES["start"]
    covered = ft.Shape(open_.points, [True, True])
    d_open, d_cov = ft.simulate(open_, LV, grob).drag, ft.simulate(covered, LV, grob).drag
    assert d_cov < 0.97 * d_open
    assert ft.seats(open_, LV) == ft.seats(covered, LV)
