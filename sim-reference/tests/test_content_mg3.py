"""MG3 „Fischfabrik“: Inhaltsdaten und Verdrahtung, die das Spiel zur Laufzeit ungeprüft liest."""
import json
import re
from pathlib import Path

ROOT = Path(__file__).resolve().parents[2]
DATA = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
SCRIPTS = ROOT / "unity/PinguinKolonie/Assets/_Project/Scripts"
MG3 = SCRIPTS / "Minigames/MG3_Fischfabrik"


def load(rel):
    return json.loads((DATA / rel).read_text(encoding="utf-8"))


DIALOGUE = load("Dialogue/dialogue_mg3_de.json")
MAIN_DIALOGUE = load("Dialogue/dialogue_de.json")
CONTENT = load("Content/mg3_content_de.json")
UI = load("Content/ui_mg3_de.json")
MAIN_UI = load("Content/ui_de.json")
LEVEL = load("Levels/MG3_Fischfabrik.json")
CODE = "".join(p.read_text(encoding="utf-8") for p in MG3.glob("*.cs"))


def test_every_line_has_a_known_speaker():
    speakers = {s["id"] for s in DIALOGUE["speakers"] + MAIN_DIALOGUE["speakers"]}
    unknown = {l["speaker"] for n in DIALOGUE["nodes"] for l in n["lines"]} - speakers
    assert not unknown


def test_dialogue_nodes_are_prefixed_and_do_not_collide():
    ids = [n["id"] for n in DIALOGUE["nodes"]]
    assert len(ids) == len(set(ids))
    assert all(i.startswith("mg3_") for i in ids)
    assert not set(ids) & {n["id"] for n in MAIN_DIALOGUE["nodes"]}
    assert not {s["id"] for s in DIALOGUE["speakers"]} & {s["id"] for s in MAIN_DIALOGUE["speakers"]}


def test_every_node_is_played():
    """Ein Knoten, den der Code nie abspielt, ist toter Text (mg3_hub_olaf ist für den Hub des Integrators)."""
    played = set(re.findall(r'"(mg3_\w+)"', CODE))
    assert {n["id"] for n in DIALOGUE["nodes"]} - played == {"mg3_hub_olaf"}


def test_ui_keys_are_prefixed_and_unique():
    keys = set(UI) - {"note", "underTheHood"}
    assert all(k.startswith("mg3") for k in keys)
    assert not keys & set(MAIN_UI)
    tech = set(UI["underTheHood"]) - {"formulas"}
    assert all(k.startswith("mg3") for k in tech)
    assert not tech & set(MAIN_UI["underTheHood"])
    formulas = set(UI["underTheHood"]["formulas"]) - {"note"}
    assert all(k.startswith("mg3_") for k in formulas)


def test_ui_placeholders_are_numbered_from_zero():
    def walk(d):
        for k, v in d.items():
            if isinstance(v, dict):
                if k != "formulas":
                    yield from walk(v)
            elif isinstance(v, str) and k != "note":
                yield k, v
    for key, text in walk(UI):
        nums = sorted({int(n) for n in re.findall(r"\{(\d+)(?:,[-\d]+)?(?::[^}]*)?\}", text)})
        assert nums == list(range(len(nums))), key
        assert text.count("{") == text.count("}"), key


def test_star_texts_exist_for_every_star_count():
    assert {f"mg3Stars{i}" for i in range(4)} <= set(UI)


def test_every_glossary_phase_is_triggered_by_the_game():
    phases = {g["phase"] for g in CONTENT["glossary"]}
    triggered = set(re.findall(r'AddGlossary\("(\w+)"\)', CODE))
    # „transfer“ (Fabrikplanung) steht wie in MG1 nur auf der Transfer-Karte
    assert phases - triggered == {"transfer"}
    assert triggered <= phases


def test_phase_bar_has_seven_steps_in_order():
    ids = [p["id"] for p in CONTENT["phases"]]
    assert ids == ["modelling", "equation", "assembly", "solution", "randomness", "optimization", "transfer"]
    calls = [int(n) for n in re.findall(r"Hud\.ShowPhases\((\d)\)", (MG3 / "FishFactoryMinigame.cs").read_text(encoding="utf-8"))]
    assert calls == list(range(6))
    assert "ShowPhases(6)" in (MG3 / "FishFactoryTransfer.cs").read_text(encoding="utf-8")


def test_cards_and_quiz_have_clear_answers():
    assert sum(c["correct"] for c in CONTENT["cards"]) == 4
    assert all(c["feedback"] for c in CONTENT["cards"] if not c["correct"])
    assert sum(o["correct"] for o in CONTENT["ruleOptions"]) == 1
    assert [o["correct"] for o in CONTENT["ruleOptions"]].index(True) != 0


def test_names_exist_for_every_tile_and_hop():
    for sym in "DSFPTKBR":
        assert CONTENT["stations"][sym]
    assert len(CONTENT["hops"]) == len(LEVEL["stages"]) + 1


def test_level_grids_are_complete_and_keep_the_hall():
    w, h = LEVEL["grid"]["width"], LEVEL["grid"]["height"]
    hall = LEVEL["initialTiles"]
    fixed = {(x, y) for y, r in enumerate(hall) for x, c in enumerate(r) if c in "DT#"}
    for name in ("initialTiles", "kalleTiles", "olafTiles", "hintTiles"):
        rows = LEVEL[name]
        assert len(rows) == h and all(len(r) == w for r in rows), name
        assert set("".join(rows)) <= set(".DT#SFPKBR"), name
        assert {(x, y) for y, r in enumerate(rows) for x, c in enumerate(r) if c in "DT#"} == fixed, name


def test_kalle_only_adds_robots_to_olafs_factory():
    diff = [(a, b) for ra, rb in zip(LEVEL["olafTiles"], LEVEL["kalleTiles"]) for a, b in zip(ra, rb) if a != b]
    assert diff and all(b == "R" for _, b in diff)


def test_kalle_attempt_runs_in_the_real_hall_before_the_lab():
    """Kalles Versuch in echt (eigene Halle, kein ZWILLI), danach Bauen auf ZWILLIS Bildschirm."""
    game = (MG3 / "FishFactoryMinigame.cs").read_text(encoding="utf-8")
    assert game.index("FishFactoryHall.Play(") < game.index("yield return PhaseBuild()")
    hall = (MG3 / "FishFactoryHall.cs").read_text(encoding="utf-8")
    assert 'Say("mg3_kalle_intro")' in hall and 'Say("mg3_kalle_after")' in hall
    assert "Hologram" not in hall and ", true," in hall  # Halle im echten Look, ohne ZWILLI


def test_lesson_follows_the_rule_in_step_2():
    game = (MG3 / "FishFactoryMinigame.cs").read_text(encoding="utf-8")
    rule = game.index("private IEnumerator PhaseRule()")
    assert rule < game.index('Hud.Say("mg3_rule_done")', rule) < game.index("StationLessonView.Play(") < game.index('AddGlossary("event")')


def test_zoom_is_switched_on_and_reset():
    game = (MG3 / "FishFactoryMinigame.cs").read_text(encoding="utf-8")
    assert "ZoomEnabled = true" in game and "ZoomEnabled = false" in game and "ResetZoom()" in game


def test_transfer_card_is_complete():
    card = load("Transfer/transfer_MG3_factory.json")
    assert card["minigame"] == "MG3_FishFactory"
    assert "Tecnomatix Plant Simulation" in card["product"]
    assert not any(card["approval"].values())
    assert len(card["body"]["de"].split()) <= 60
