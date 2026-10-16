import importlib.util
from pathlib import Path

spec = importlib.util.spec_from_file_location(
    "text_lint", Path(__file__).resolve().parents[2] / "tools/text_lint.py")
text_lint = importlib.util.module_from_spec(spec)
spec.loader.exec_module(text_lint)


def test_standard_texts_follow_language_rules():
    assert text_lint.lint() == []


import pytest


@pytest.mark.parametrize("probe", [
    "Der Löser rechnet weiter",          # deutsches Wort für Solver
    "Das Ergebnis konvergiert",          # Verbform von Konvergenz
    "Die Rechenmatrix ist fertig",       # zusammengesetztes Wort
    "Unser Simulationssolver",
    "T_i steigt um dT/dt",               # Formelzeichen ohne =
    "Wärme × Fläche",
    "Ab 0 °C schmilzt es",               # Einheit
    "Zwei Sterne ★★ geschafft",          # Zeichen außerhalb der eingebauten Schrift
    "Mehr Laternen ▲",
])
def test_lint_catches_what_slipped_through_before(probe):
    """Alle diese Texte gingen in v0.1 durch den Lint (Review zu PR #2)."""
    assert text_lint.check_text(probe, "probe"), probe


@pytest.mark.parametrize("ok", [
    "Ein Modell ist ein vereinfachtes Abbild – „stark vereinfacht“, sagt Frieda …",
    "Smart Infrastructure · Digital Industries Software",
    "Runde 2 von 3",
])
def test_lint_accepts_plain_german(ok):
    assert text_lint.check_text(ok, "ok") == []


def test_lint_finds_game_text_in_csharp(tmp_path):
    (tmp_path / "A.cs").write_text(
        'Hud.Toast("Bau zuerst ein paar Iglus.", 3f);\n'
        'var l = UIKit.Label(rt, $"Runde {n} von {m}", 26, c);\n'
        'var ok = UIKit.Label(lamp.transform, "OK", 28, c);\n'
        'Hud.Toast(_root.Data.Ui("needIgloos"), 3f);\n'
        '// Hud.Toast("auskommentiert");\n', encoding="utf-8")
    found = text_lint.lint_csharp(tmp_path)
    assert len(found) == 2, found


def test_every_text_the_code_asks_for_exists():
    """Ein Tippfehler im Schlüssel zeigt im Spiel nur den Schlüssel – das fällt erst im Editor auf."""
    assert text_lint.lint_references() == []


def test_reference_check_spots_a_missing_key(tmp_path):
    scripts = tmp_path / "Scripts"
    scripts.mkdir()
    (scripts / "A.cs").write_text('Hud.Toast(Data.Ui("gibtEsNicht"), 3f);\nyield return Hud.Say("hub_intro");\n', encoding="utf-8")
    found = text_lint.lint_references(scripts)
    assert len(found) == 1 and "gibtEsNicht" in found[0]
