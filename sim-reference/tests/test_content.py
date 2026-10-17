"""Inhaltsdaten, die das Spiel zur Laufzeit ungeprüft liest – ein Fehler hier fällt sonst erst im Editor auf."""
import json
import re
from pathlib import Path

import pytest

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
SCRIPTS = ROOT / "unity/PinguinKolonie/Assets/_Project/Scripts"


def load(rel):
    return json.loads((DATA / rel).read_text(encoding="utf-8"))


@pytest.fixture(scope="module")
def dialogue():
    return load("Dialogue/dialogue_de.json")


def test_every_line_has_a_known_speaker(dialogue):
    speakers = {s["id"] for s in dialogue["speakers"]}
    unknown = {l["speaker"] for n in dialogue["nodes"] for l in n["lines"]} - speakers
    assert not unknown


def test_dialogue_ids_are_unique(dialogue):
    ids = [n["id"] for n in dialogue["nodes"]]
    assert len(ids) == len(set(ids))


def test_speaker_colours_are_valid_hex(dialogue):
    for s in dialogue["speakers"]:
        assert re.fullmatch(r"#[0-9A-Fa-f]{6}", s["color"]), s


def test_new_characters_each_have_a_hub_line(dialogue):
    """Theo, Klara und Flocke sind im Hub ansprechbar (Spec 002 UX-25)."""
    ids = {n["id"] for n in dialogue["nodes"]}
    assert {"hub_theo", "hub_klara", "hub_flocke"} <= ids


def test_no_line_claims_the_computer_is_exact(dialogue):
    """Review zu PR #2: „genau voraus“ widersprach Lernziel 5 – ein Modell ist eine Vereinfachung."""
    for n in dialogue["nodes"]:
        for l in n["lines"]:
            assert "genau voraus" not in l["text"], n["id"]


CARDS = sorted((DATA / "Transfer").glob("*.json"))


@pytest.fixture(scope="module")
def recall():
    """Alle „Kurz nachgedacht“-Fragen aller Transfer-Karten (Schaltschrank, Akku, …)."""
    return [q for f in CARDS for q in json.loads(f.read_text(encoding="utf-8")).get("retrieval", [])]


def test_each_recall_question_has_exactly_one_right_answer(recall):
    for q in recall:
        assert sum(o["correct"] for o in q["options"]) == 1, q["question"]
        assert all(o["feedback"]["de"] for o in q["options"])


def test_right_answer_is_not_always_in_the_same_place(recall):
    """QA-Befund: Stand die richtige Antwort immer oben, lernte man „oben ist richtig“."""
    positions = {[o["correct"] for o in q["options"]].index(True) for q in recall}
    assert len(positions) > 1


@pytest.mark.parametrize("card", CARDS, ids=lambda f: f.stem)
def test_every_transfer_card_is_a_draft_until_approved(card):
    """Siemens-Inhalte sind Vorschläge: ohne vollständige Freigabe zeigt das Spiel „ENTWURF“ (UX-11)."""
    d = json.loads(card.read_text(encoding="utf-8"))
    for key in ("headline", "body", "takeaway", "translation", "approval", "product", "siemensBusiness"):
        assert key in d, key
    assert not all(d["approval"].values()), "Freigabe eingetragen – bitte bewusst prüfen und diesen Test anpassen"


def test_both_transfer_screens_are_reached():
    """Nach dem Huddle der Akku, nach MG1 der Schaltschrank (Spieltest 17.10.2026: der erste fehlte)."""
    code = (SCRIPTS / "Core/GameRoot.cs").read_text(encoding="utf-8")
    assert "TransferScreen.PlayBattery(this)" in code
    assert "TransferScreen.Play(this)" in code
    assert code.index("PrologueHuddle.Create") < code.index("PlayBattery") < code.index("SettlementMinigame.Create")


def test_level_grids_are_complete():
    lv = load("Levels/MG1_Huddle_Settlement.json")
    w, h = lv["grid"]["width"], lv["grid"]["height"]
    grids = {"initialTiles": lv["initialTiles"], "kalleTiles": lv["kalleTiles"], "hintTiles": lv["hintTiles"]}
    grids.update({f"whatIf.{k}": v["tiles"] for k, v in lv["whatIf"].items()})
    for name, rows in grids.items():
        assert len(rows) == h, name
        assert all(len(r) == w for r in rows), name
        assert set("".join(rows)) <= set(".ILW=~"), name


def test_every_glossary_phase_is_triggered_by_the_game():
    """Ein Begriff, dessen Phase der Code nie meldet, landet nie im Notizbuch."""
    phases = {g["phase"] for g in load("Content/mg1_content_de.json")["glossary"]}
    code = "".join(p.read_text(encoding="utf-8") for p in SCRIPTS.rglob("*.cs"))
    triggered = set(re.findall(r'AddGlossary\("(\w+)"\)', code))
    # „transfer“ (Schaltschrank) ist seit v0.1 nur auf der Transfer-Karte erklärt – bewusst nicht im Notizbuch.
    assert phases - triggered == {"transfer"}


def test_ui_placeholders_are_numbered_from_zero():
    """string.Format wirft zur Laufzeit, wenn {1} ohne {0} steht oder Klammern nicht passen."""
    ui = load("Content/ui_de.json")
    def walk(d):
        for k, v in d.items():
            if isinstance(v, dict):
                yield from walk(v)
            elif isinstance(v, str) and k != "note":
                yield k, v
    for key, text in walk(ui):
        nums = sorted({int(n) for n in re.findall(r"\{(\d+)(?::[^}]*)?\}", text)})
        assert nums == list(range(len(nums))), key
        assert text.count("{") == text.count("}"), key
