"""Text-Lint für alle Standardmodus-Texte (docs/04 Abschnitt 5.6, docs/00 Abschnitt 5).

Prüft alle Spieltexte – die JSON-Dateien unter unity/PinguinKolonie/Assets/_Project/Resources/Data
(Dialoge, Inhalte, Oberfläche, Transfer-Karten) – und dass im C#-Code keine Spieltexte stehen:
  - keine Fachwörter und Abkürzungen aus der Sperrliste, auch nicht in zusammengesetzten Wörtern
  - keine Formelzeichen und keine Einheiten
  - nur Zeichen, die die eingebaute Schrift sicher hat (keine Emoji, kein ★ ✔ ▸ ▲ ▼)
  - Sprechblasen höchstens ~2 Zeilen (MAX_LINE_CHARS Zeichen)
  - Transfer-Text höchstens 60 Wörter
  - Spieltexte nur in JSON: ein Textliteral in Toast/Label/Button im C#-Code ist ein Verstoß

Ausgenommen sind Abschnitte `underTheHood` – sie erscheinen nur im Modus „Blick unter die Haube“.

Aufruf:  python tools/text_lint.py      (Exit-Code 1 bei Verstößen)
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
SCRIPTS = ROOT / "unity/PinguinKolonie/Assets/_Project/Scripts"
# Ordner mit Spieltexten. Levels/ und Materials/ enthalten nur Zahlen und Entwickler-Notizen.
TEXT_FOLDERS = ["Dialogue", "Content", "Transfer"]

# Wörter: Groß-/Kleinschreibung egal, auch als Teil eines Wortes („Rechenmatrix“, „Simulationssolver“).
# Darum stehen hier Wortstämme; zu kurze Stämme würden harmlose Wörter treffen und fehlen bewusst.
FORBIDDEN_WORDS = ["Matrix", "Matrizen", "Diskretisier", "Gleichungssystem", "Solver", "Löser",
                   "Iteration", "Residu", "Finite", "konjugiert", "Konvergenz", "konvergier", "Gradient",
                   "Differentialgleichung", "Randbedingung", "Algorithm", "Parameter", "Unbekannte"]
# Abkürzungen: nur exakt in Großbuchstaben als eigenes Wort (sonst wäre „des“ verboten).
FORBIDDEN_ABBREVIATIONS = ["FEM", "CFD", "DES", "CBTC", "LBM", "CG", "PDE", "FVM"]
FORMULA = re.compile(r"[=∑Σ∂∫^²³√≤≥×÷±∆Δ<>]|\b[A-Za-z]_[A-Za-z0-9]|\bd[A-Z]/dt\b")
UNITS = re.compile(r"°|\b\d+\s?(K|W|kW|m|cm|mm|s|ms)\b")
# Sicher in der eingebauten Schrift: Latin-1 plus die deutsche Typografie, die die Texte nutzen.
SAFE_EXTRA = set("„“‚‘’–—…•€")
MAX_LINE_CHARS = 140
MAX_TRANSFER_WORDS = 60
# Felder, die nicht im Standardmodus angezeigt werden oder keine Spieltexte sind
SKIP_KEYS = {"id", "speaker", "side", "phase", "color", "minigame", "placeholder", "language", "product",
             "underTheHood", "note", "look", "trigger"}

# C#: Aufrufe, deren erstes Textargument auf dem Bildschirm landet.
CS_UI_CALL = re.compile(r"\b(Toast|Label|Button|SetButtonLabel)\(\s*(?:[\w.\[\]]+\s*,\s*)?(\$?@?\"(?:[^\"\\]|\\.)*\")")
CS_TEXT_ASSIGN = re.compile(r"\.text\s*=\s*(\$?@?\"(?:[^\"\\]|\\.)*\")")
# Ein Literal ohne Wort aus Buchstaben (nur Zeichen wie "!" oder "OK") ist kein Spieltext.
CS_ALLOWED_LITERALS = {'"OK"', '"X"', '"!"', '""'}


def strings(node, path=""):
    if isinstance(node, dict):
        for k, v in node.items():
            if k in SKIP_KEYS:
                continue
            yield from strings(v, f"{path}.{k}")
    elif isinstance(node, list):
        for i, v in enumerate(node):
            yield from strings(v, f"{path}[{i}]")
    elif isinstance(node, str):
        yield path, node


def check_text(text: str, where: str) -> list[str]:
    """Regeln, die für jeden Standardtext gelten – egal ob Dialog, Knopf oder Hinweis."""
    problems = []
    low = text.lower()
    for w in FORBIDDEN_WORDS:
        if w.lower() in low:
            problems.append(f"{where}: Fachwort '{w}' im Standardtext: {text!r}")
    for a in FORBIDDEN_ABBREVIATIONS:
        if re.search(rf"\b{a}\b", text):
            problems.append(f"{where}: Abkürzung '{a}' im Standardtext: {text!r}")
    if FORMULA.search(text):
        problems.append(f"{where}: Formelzeichen im Standardtext: {text!r}")
    if UNITS.search(text):
        problems.append(f"{where}: Einheit im Standardtext: {text!r}")
    odd = sorted({c for c in text if ord(c) > 255 and c not in SAFE_EXTRA})
    if odd:
        problems.append(f"{where}: Zeichen, die die eingebaute Schrift evtl. nicht hat: {''.join(odd)!r}")
    return problems


def lint_json() -> list[str]:
    problems = []
    for folder in TEXT_FOLDERS:
        for file in sorted((DATA / folder).glob("*.json")):
            rel = f"{folder}/{file.name}"
            doc = json.loads(file.read_text(encoding="utf-8"))
            for path, text in strings(doc):
                where = f"{rel}{path}"
                problems += check_text(text, where)
                if ".lines[" in path and len(text) > MAX_LINE_CHARS:
                    problems.append(f"{where}: Sprechblase zu lang ({len(text)} > {MAX_LINE_CHARS} Zeichen)")
            if folder == "Transfer":
                words = len(doc["body"]["de"].split())
                if words > MAX_TRANSFER_WORDS:
                    problems.append(f"{rel}: Transfer-Text hat {words} Wörter (max. {MAX_TRANSFER_WORDS})")
    return problems


def lint_csharp(root: Path = SCRIPTS) -> list[str]:
    """Spieltexte gehören in JSON (CLAUDE.md). Sonst prüft sie niemand gegen die Sprachregeln."""
    problems = []
    for file in sorted(root.rglob("*.cs")):
        if "Editor" in file.relative_to(root).parts:
            continue  # Editor-Menüs sieht kein Spieler
        for n, line in enumerate(file.read_text(encoding="utf-8").splitlines(), 1):
            if line.lstrip().startswith("//"):
                continue
            for m in list(CS_UI_CALL.finditer(line)) + list(CS_TEXT_ASSIGN.finditer(line)):
                literal = m.group(m.lastindex)
                if literal in CS_ALLOWED_LITERALS:
                    continue
                body = re.sub(r"\{[^}]*\}", "", literal)  # Platzhalter in $"…" zählen nicht
                if re.search(r"[A-Za-zÄÖÜäöüß]{2,}", body):
                    rel = file.relative_to(root.parent if root == SCRIPTS else root).as_posix()
                    problems.append(f"{rel}:{n}: Spieltext im C#-Code statt in Resources/Data: {literal}")
    return problems


# Schlüssel, die der Code zusammensetzt (z. B. "stars" + Anzahl) und die die Suche darum nicht findet.
DYNAMIC_UI_KEYS = ["stars0", "stars1", "stars2", "stars3", "klaraBetter", "klaraCheaper", "klaraWorse", "klaraSame",
                   "hubReplayHint1", "hubReplayHint2", "hubReplayHint3"]
CS_REFERENCES = {
    # Nur vollständige Schlüssel: Auf das schließende Anführungszeichen folgt ',' oder ')' – nicht '+'.
    "Ui": re.compile(r"\bUi\(\s*\"(\w+)\"\s*[,)]"),
    "Tech": re.compile(r"\bTech\(\s*\"(\w+)\"\s*[,)]"),
    "Say": re.compile(r"\bSay\(\s*\"(\w+)\"\s*[,)]"),
    "Content": re.compile(r"\bContent\(\s*\"(\w+)\"\s*[,)]"),
    "IdleNode": re.compile(r"\bSetIdleNode\(\s*\"\w+\"\s*,\s*\"(\w+)\""),
    "SayChoice": re.compile(r"Say\([^;]*?\?\s*\"(\w+)\"\s*:\s*\"(\w+)\""),
}


def lint_references(root: Path = SCRIPTS) -> list[str]:
    """Jeder Text, den der Code anfordert, muss es geben – sonst steht im Spiel nur der Schlüssel."""
    # Wie GameData.Load: ui_de.json plus ui_<minigame>_de.json, alle Dialogdateien, alle *_content_de.json
    ui, tech, content, say = set(), set(), set(), set()
    for file in sorted((DATA / "Content").glob("ui_*.json")):
        doc = json.loads(file.read_text(encoding="utf-8"))
        ui |= set(doc)
        tech |= set(doc.get("underTheHood", {}))
    for file in sorted((DATA / "Content").glob("*_content_de.json")):
        content |= set(json.loads(file.read_text(encoding="utf-8")))
    for file in sorted((DATA / "Dialogue").glob("*.json")):
        say |= {n["id"] for n in json.loads(file.read_text(encoding="utf-8"))["nodes"]}
    known = {"Ui": ui, "Tech": tech, "Content": content, "Say": say}
    known["IdleNode"] = known["SayChoice"] = known["Say"]
    problems = [f"ui_de.json: dynamischer Schlüssel '{k}' fehlt" for k in DYNAMIC_UI_KEYS if k not in known["Ui"]]
    for file in sorted(root.rglob("*.cs")):
        text = file.read_text(encoding="utf-8")
        for kind, pattern in CS_REFERENCES.items():
            for m in pattern.finditer(text):
                for key in m.groups():
                    if key not in known[kind]:
                        line = text.count("\n", 0, m.start()) + 1
                        problems.append(f"{file.relative_to(root.parent).as_posix()}:{line}: {kind}(\"{key}\") gibt es nicht")
    return problems


def lint() -> list[str]:
    return lint_json() + lint_csharp() + lint_references()


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8", errors="replace")  # Windows-Konsole läuft unter cp1252
    found = lint()
    for p in found:
        print("FEHLER", p)
    print(f"{len(found)} Verstöße" if found else "Text-Lint: alles in Ordnung")
    sys.exit(1 if found else 0)
