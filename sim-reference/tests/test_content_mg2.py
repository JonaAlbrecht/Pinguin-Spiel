"""Inhaltsdaten von MG2 „Polar-Express“, die das Spiel zur Laufzeit ungeprüft liest (wie test_content.py für MG1)."""
import json
import re
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
SCRIPTS = ROOT / "unity/PinguinKolonie/Assets/_Project/Scripts"
MG2 = SCRIPTS / "Minigames/MG2_PolarExpress"


def load(rel):
    return json.loads((DATA / rel).read_text(encoding="utf-8"))


def code():
    return "".join(p.read_text(encoding="utf-8") for p in sorted(MG2.glob("*.cs")))


@pytest.fixture(scope="module")
def dialogue():
    return load("Dialogue/dialogue_mg2_de.json")


@pytest.fixture(scope="module")
def content():
    return load("Content/mg2_content_de.json")


@pytest.fixture(scope="module")
def ui():
    return load("Content/ui_mg2_de.json")


def test_every_line_has_a_known_speaker(dialogue):
    known = {s["id"] for s in load("Dialogue/dialogue_de.json")["speakers"]} | {s["id"] for s in dialogue["speakers"]}
    unknown = {l["speaker"] for n in dialogue["nodes"] for l in n["lines"]} - known
    assert not unknown


def test_new_speakers_do_not_redefine_existing_ones(dialogue):
    existing = {s["id"] for s in load("Dialogue/dialogue_de.json")["speakers"]}
    assert not existing & {s["id"] for s in dialogue["speakers"]}
    for s in dialogue["speakers"]:
        assert re.fullmatch(r"#[0-9A-Fa-f]{6}", s["color"]), s


def test_dialogue_ids_are_prefixed_unique_and_do_not_collide(dialogue):
    ids = [n["id"] for n in dialogue["nodes"]]
    assert len(ids) == len(set(ids))
    assert all(i.startswith("mg2_") for i in ids)
    assert not set(ids) & {n["id"] for n in load("Dialogue/dialogue_de.json")["nodes"]}


def test_every_node_the_code_plays_exists(dialogue):
    """Auch die zusammengesetzten: mg2_phase3_<Auflösung>."""
    ids = {n["id"] for n in dialogue["nodes"]}
    c = code()
    said = set(re.findall(r'Say\("(mg2_\w+)"\)', c))
    assert said - ids == set()
    # Erwähnt (auch in Bedingungen und als Argument für den Transfer) oder zusammengesetzt
    mentioned = set(re.findall(r'"(mg2_\w+)"', c))
    level = load("Levels/MG2_PolarExpress.json")
    mentioned |= {f"mg2_phase3_{r['id']}" for r in level["resolutions"]}
    assert ids - mentioned == set(), "Dialogknoten, die nie gespielt werden"


def test_ui_keys_are_prefixed_and_do_not_collide(ui):
    base = load("Content/ui_de.json")
    for k in ui:
        assert k == "underTheHood" or k.startswith("mg2"), k
        assert k == "underTheHood" or k not in base, k
    for k in ui["underTheHood"]:
        assert k == "formulas" or k.startswith("mg2"), k
        assert k == "formulas" or k not in base["underTheHood"], k
    for k in ui["underTheHood"]["formulas"]:
        assert k.startswith("mg2_") and k not in base["underTheHood"]["formulas"], k


def test_dynamic_ui_keys_exist(ui):
    level = load("Levels/MG2_PolarExpress.json")
    for r in level["resolutions"]:
        assert f"mg2Res_{r['id']}" in ui
    for s in range(4):
        assert f"mg2Stars{s}" in ui


def test_formulas_used_by_the_code_exist(ui):
    used = set(re.findall(r'"(mg2_[a-z]+)"', code())) - {n["id"] for n in load("Dialogue/dialogue_mg2_de.json")["nodes"]}
    assert used and used <= set(ui["underTheHood"]["formulas"])


def test_ui_placeholders_are_numbered_from_zero(ui):
    def walk(d):
        for k, v in d.items():
            if isinstance(v, dict):
                yield from walk(v)
            elif isinstance(v, str) and k not in ("formulas",):
                yield k, v
    for key, text in walk({k: v for k, v in ui.items()}):
        if key.startswith("mg2_"):
            continue  # LaTeX-Quellen
        nums = sorted({int(n) for n in re.findall(r"\{(\d+)(?::[^}]*)?\}", text)})
        assert nums == list(range(len(nums))), key
        assert text.count("{") == text.count("}"), key


def test_cards_follow_docs_03(content):
    """Form der Nase, Gegenwind, Übergänge richtig – Farbe der Lok, Anzahl der Fenster falsch (mit Hinweis)."""
    cards = {c["id"]: c for c in content["cards"]}
    assert {k for k, c in cards.items() if c["correct"]} == {"nose", "headwind", "transitions"}
    assert all(c["feedback"] for c in cards.values() if not c["correct"])
    assert len(content["phases"]) == 6


def test_hand_question_has_exactly_one_right_answer(content):
    opts = content["hand"]["options"]
    assert sum(o["correct"] for o in opts) == 1
    assert all(o["feedback"] for o in opts)


def test_every_glossary_phase_is_triggered(content):
    phases = {g["phase"] for g in content["glossary"]}
    c = code()
    triggered = set(re.findall(r'AddGlossary\("(\w+)"\)', c))
    if '"transfer"' in (MG2 / "PolarExpressTransfer.cs").read_text(encoding="utf-8"):
        triggered.add("transfer")
    assert phases == triggered


def test_transfer_card_is_a_draft_with_a_takeaway():
    card = load("Transfer/transfer_MG2_velaro.json")
    assert not any(card["approval"].values())
    assert "Vorschlag" in card["product"]
    assert card["siemensBusiness"] == "Siemens Mobility"
    assert card["takeaway"]["de"].startswith("Bevor ein Zug gebaut wird")
    assert len(card["translation"]) == 4
    assert "transfer_MG2_velaro" in (MG2 / "PolarExpressTransfer.cs").read_text(encoding="utf-8")


def test_kalle_tries_outside_before_the_lab():
    """Kalles Versuch in echt läuft draußen (eigene Szene), erst danach entsteht ZWILLIS Windkanal."""
    lab = (MG2 / "PolarExpressMinigame.cs").read_text(encoding="utf-8")
    assert lab.index("PlayKalleAttempt(") < lab.index("FlowBoard.Create(")
    assert 'Say("mg2_kalle_intro")' not in lab
    outdoor = (MG2 / "PolarExpressOutdoor.cs").read_text(encoding="utf-8")
    assert 'Say("mg2_kalle_intro")' in outdoor and 'Say("mg2_kalle_after")' in outdoor
    assert "Grid" not in outdoor and "Hologram" not in outdoor   # draußen kein Raster, kein ZWILLI


def test_lesson_table_follows_the_rule_in_step_2():
    lab = (MG2 / "PolarExpressMinigame.cs").read_text(encoding="utf-8")
    rule = lab.index("private IEnumerator PhaseRule()")
    res = lab.index("private IEnumerator PhaseResolution()")
    assert rule < lab.index('Say("mg2_phase2_done")', rule) < lab.index("RowLessonView.Play(") < lab.index('AddGlossary("timestep")') < res


def test_zoom_is_switched_on_and_reset():
    lab = (MG2 / "PolarExpressMinigame.cs").read_text(encoding="utf-8")
    assert "ZoomEnabled = true" in lab and "ZoomEnabled = false" in lab and "ResetZoom()" in lab


def test_level_shapes_and_lesson_exist():
    lv = load("Levels/MG2_PolarExpress.json")
    assert {"kalle", "start", "hint"} <= set(lv["shapes"])
    for s in lv["shapes"].values():
        assert len(s["nose"]) == 4 and len(s["covered"]) == len(lv["train"]["transitions"])
    les = lv["lesson"]
    assert len(les["air"]) == len(les["push"]) == 5
    assert lv["playResolution"] in {r["id"] for r in lv["resolutions"]}
