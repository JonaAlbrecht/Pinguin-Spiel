"""Stil-Lint: Farben nur aus der Palette, Palette nur aus dem Siemens-Farbschema (docs/07_Stilregeln.md).

Prüft
  - im C#-Code der Spielschicht kein `new Color(...)`, `new Color32(...)` und keine Unity-Grundfarben
    (Color.black, Color.red …) außerhalb von Palette.cs und ArtStyle.cs. Erlaubt sind Color.white und Color.clear
    (neutrale Tönung bzw. unsichtbar). Eine Rechnung mit Farbkanälen markiert man mit `// Stil-Lint: Rechnung`.
  - in Palette.Siemens nur Werte, die im Siemens-Farbschema stehen (Liste unten, sie-colors-overview V1.4.4).
  - die Sprecherfarben in Resources/Data/Dialogue/*.json stammen aus dem Siemens-Farbschema.

Aufruf:  python tools/style_lint.py      (Exit-Code 1 bei Verstößen)
"""
from __future__ import annotations

import json
import re
import sys
from pathlib import Path

ROOT = Path(__file__).resolve().parent.parent
PROJECT = ROOT / "unity/PinguinKolonie/Assets/_Project"
SCRIPTS = PROJECT / "Scripts"
PALETTE = SCRIPTS / "Core/Palette.cs"
ALLOWED_FILES = {PALETTE.name, "ArtStyle.cs"}

# Siemens-Farbschema „sie-colors-overview“ V1.4.4 (Stand 2025-02-17), Bildschirmwerte: Primär-, Sekundär- und
# Zusatzfarben. Haut- und Haartöne sind bewusst nicht dabei – im Spiel gibt es keine Menschen.
SIEMENS = {
    "009999", "00C1B6", "00FFB9", "00D7A0", "00E6DC", "00BEDC", "000028", "F3F3F0",
    "AAAA96", "C5C5B8", "DFDFD9", "F7C600", "FFD732", "FFE270", "00646E", "00AF8E", "00557C", "0087BE",
    "553BA3", "805CFF", "B4A8FF", "333353", "66667E", "9999A9", "CCCCD4", "E5E5E9", "EF0137", "EC6602", "FF9000",
    "0D0D33", "19193D", "262648", "4C4C68", "737389", "808099", "B3B3BE", "EBEBEE",
}

LITERAL = re.compile(r"new\s+Color(32)?\s*\(|\bColor\.(black|red|green|blue|yellow|cyan|magenta|gray|grey)\b")
EXEMPT = "Stil-Lint: Rechnung"
HEX = re.compile(r"Hex\(0x([0-9A-Fa-f]{6})\)")


def check_code() -> list[str]:
    problems = []
    for f in sorted(SCRIPTS.rglob("*.cs")):
        if f.name in ALLOWED_FILES:
            continue
        for n, line in enumerate(f.read_text(encoding="utf-8").splitlines(), 1):
            code = line.split("//", 1)[0] if EXEMPT not in line else ""
            if LITERAL.search(code):
                problems.append(f"{f.relative_to(ROOT)}:{n}: Farbe außerhalb der Palette: {line.strip()}")
    return problems


def check_palette() -> list[str]:
    text = PALETTE.read_text(encoding="utf-8")
    start = text.find("class Siemens")
    end = text.find("\n        }", start)
    found = HEX.findall(text[start:end])
    problems = [f"{PALETTE.relative_to(ROOT)}: #{h.upper()} steht nicht im Siemens-Farbschema"
                for h in found if h.upper() not in SIEMENS]
    if not found:
        problems.append(f"{PALETTE.relative_to(ROOT)}: keine Siemens-Farben gefunden (class Siemens mit Hex(0x……))")
    for line in text[end:].splitlines():
        if "Hex(0x" in line:
            problems.append(f"{PALETTE.relative_to(ROOT)}: Hex-Farbe außerhalb von Palette.Siemens: {line.strip()}")
    return problems


def check_speakers() -> list[str]:
    problems = []
    for f in sorted((PROJECT / "Resources/Data/Dialogue").glob("*.json")):
        for sp in json.loads(f.read_text(encoding="utf-8")).get("speakers", []):
            c = sp.get("color", "").lstrip("#").upper()
            if c not in SIEMENS:
                problems.append(f"{f.relative_to(ROOT)}: Sprecher {sp.get('id')}: #{c} steht nicht im Siemens-Farbschema")
    return problems


def main() -> int:
    problems = check_code() + check_palette() + check_speakers()
    for p in problems:
        print(p)
    if problems:
        print(f"Stil-Lint: {len(problems)} Verstoß/Verstöße")
        return 1
    print("Stil-Lint: alles in Ordnung")
    return 0


if __name__ == "__main__":
    sys.exit(main())
