"""Text-Lint für alle Standardmodus-Texte (docs/04 Abschnitt 5.6, docs/00 Abschnitt 5).

Prüft Dialoge, Minigame-Inhalte und Transfer-Karten in unity/PinguinKolonie/Assets/_Project/Resources/Data:
  - keine Fachwörter und Abkürzungen aus der Sperrliste
  - keine Formelzeichen
  - Sprechblasen höchstens ~2 Zeilen (MAX_LINE_CHARS Zeichen)
  - Transfer-Text höchstens 60 Wörter

Aufruf:  python tools/text_lint.py      (Exit-Code 1 bei Verstößen)
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
DATA = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
FILES = ["Dialogue/dialogue_de.json", "Content/mg1_content_de.json", "Transfer/transfer_MG1_switchgear.json"]

# Wörter: Groß-/Kleinschreibung egal. Abkürzungen: nur exakt in Großbuchstaben (sonst wäre „des“ verboten).
FORBIDDEN_WORDS = ["Matrix", "Matrizen", "Diskretisierung", "diskretisieren", "Gleichungssystem", "Solver",
                   "Iteration", "Residuum", "Finite", "konjugiert", "Konvergenz", "Differentialgleichung",
                   "Randbedingung", "Algorithmus", "Parameter"]
FORBIDDEN_ABBREVIATIONS = ["FEM", "CFD", "DES", "CBTC", "LBM", "CG", "PDE", "FVM"]
FORMULA = re.compile(r"[=∑Σ∂∫^²³√≤≥]")
MAX_LINE_CHARS = 140
MAX_TRANSFER_WORDS = 60
# Felder, die nicht im Standardmodus angezeigt werden oder keine Spieltexte sind
SKIP_KEYS = {"id", "speaker", "side", "phase", "color", "minigame", "placeholder", "language", "product"}


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


def lint() -> list[str]:
    problems = []
    for rel in FILES:
        doc = json.loads((DATA / rel).read_text(encoding="utf-8"))
        for path, text in strings(doc):
            where = f"{rel}{path}"
            for w in FORBIDDEN_WORDS:
                if re.search(rf"\b{re.escape(w)}\w*", text, re.IGNORECASE):
                    problems.append(f"{where}: Fachwort '{w}' im Standardtext: {text!r}")
            for a in FORBIDDEN_ABBREVIATIONS:
                if re.search(rf"\b{a}\b", text):
                    problems.append(f"{where}: Abkürzung '{a}' im Standardtext: {text!r}")
            if FORMULA.search(text):
                problems.append(f"{where}: Formelzeichen im Standardtext: {text!r}")
            if ".lines[" in path and len(text) > MAX_LINE_CHARS:
                problems.append(f"{where}: Sprechblase zu lang ({len(text)} > {MAX_LINE_CHARS} Zeichen)")
        if rel.startswith("Transfer/"):
            words = len(doc["body"]["de"].split())
            if words > MAX_TRANSFER_WORDS:
                problems.append(f"{rel}: Transfer-Text hat {words} Wörter (max. {MAX_TRANSFER_WORDS})")
    return problems


if __name__ == "__main__":
    found = lint()
    for p in found:
        print("FEHLER", p)
    print(f"{len(found)} Verstöße" if found else "Text-Lint: alles in Ordnung")
    sys.exit(1 if found else 0)
