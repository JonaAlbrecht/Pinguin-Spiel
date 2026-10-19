"""Setzt die Formeln für „Blick unter die Haube“ wie LaTeX und legt sie als Bilder für Unity ab.

Die eingebaute Unity-Schrift kann weder Brüche noch Hoch- und Tiefstellung. Darum stehen die Formeln als
LaTeX-Quelle in ui_de.json (underTheHood.formulas) und werden hier mit matplotlib-Mathtext (Schrift Computer Modern,
wie LaTeX) gesetzt. Kein LaTeX-System nötig.

Ausgabe je Formel: Resources/Formulas/<name>.bytes – ein Graustufenbild (nur Deckkraft, Schrift weiß), damit Unity
es ohne Bildimport und ohne Zusatzpaket lädt (UIKit.Formula) und beliebig einfärbt. Aufbau:
    Breite (uint32, little endian) · Höhe (uint32) · Deckkraft zeilenweise von oben, roh deflate-komprimiert

Aufruf:
    python tools/render_formulas.py            Bilder neu erzeugen
    python tools/render_formulas.py --check    Exit-Code 1, wenn ein Bild fehlt, veraltet ist oder übrig bleibt
    python tools/render_formulas.py --preview DIR   zusätzlich PNG-Vorschauen nach DIR
"""
from __future__ import annotations

import argparse
import io
import json
import struct
import sys
import uuid
import zlib
from pathlib import Path

import matplotlib

matplotlib.use("Agg")
matplotlib.rcParams["mathtext.fontset"] = "cm"  # Computer Modern, die LaTeX-Schrift
import matplotlib.image as mpimg  # noqa: E402
from matplotlib.figure import Figure  # noqa: E402

ROOT = Path(__file__).resolve().parent.parent
RESOURCES = ROOT / "unity/PinguinKolonie/Assets/_Project/Resources"
UI_TEXT = RESOURCES / "Data/Content/ui_de.json"
OUT = RESOURCES / "Formulas"
# Ein Geviert ≈ FONT_PT * DPI / 72 Pixel. Unity skaliert beim Anzeigen (UIKit.Formula, Faktor 0,5 ≈ 1 Geviert = 33 px
# bei 1920 × 1080), das Bild ist also doppelt so fein wie die Anzeige – bleibt auf großen Bildschirmen scharf.
FONT_PT = 24
DPI = 200


def formulas() -> dict[str, str]:
    """Formeln aus ui_de.json und den Texten der Minigames (ui_mg2_de.json …); ein Name darf nur einmal vorkommen."""
    found: dict[str, str] = {}
    for file in [UI_TEXT] + sorted(p for p in UI_TEXT.parent.glob("ui_*.json") if p != UI_TEXT):
        doc = json.loads(file.read_text(encoding="utf-8"))
        for k, v in doc.get("underTheHood", {}).get("formulas", {}).items():
            if k == "note":
                continue
            if k in found:
                raise SystemExit(f"Formel {k} doppelt ({file.name})")
            found[k] = v
    return found


def render(tex: str) -> tuple[int, int, bytes, bytes]:
    """Gibt Breite, Höhe, Deckkraft (zeilenweise von oben) und eine PNG-Vorschau zurück."""
    fig = Figure()
    fig.text(0, 0, f"${tex}$", fontsize=FONT_PT, color="white")
    png = io.BytesIO()
    fig.savefig(png, format="png", dpi=DPI, transparent=True, bbox_inches="tight", pad_inches=0.04,
                metadata={"Software": None})
    rgba = mpimg.imread(io.BytesIO(png.getvalue()), format="png")
    h, w = rgba.shape[:2]
    alpha = (rgba[:, :, 3] * 255 + 0.5).astype("uint8").tobytes()
    return w, h, alpha, png.getvalue()


def encode(w: int, h: int, alpha: bytes) -> bytes:
    z = zlib.compressobj(9, zlib.DEFLATED, -15)  # roh deflate, wie System.IO.Compression.DeflateStream
    return struct.pack("<II", w, h) + z.compress(alpha) + z.flush()


def ensure_meta(path: Path, folder: bool = False) -> None:
    """Unity-Metadatei mit fester GUID, damit nicht jeder Rechner beim Öffnen eine eigene erzeugt."""
    meta = path.with_name(path.name + ".meta")
    if meta.exists():
        return
    importer = "folderAsset: yes\nDefaultImporter" if folder else "TextScriptImporter"
    meta.write_text(f"fileFormatVersion: 2\nguid: {uuid.uuid4().hex}\n{importer}:\n  externalObjects: {{}}\n"
                    "  userData: \n  assetBundleName: \n  assetBundleVariant: \n", encoding="utf-8", newline="\n")


def main() -> int:
    ap = argparse.ArgumentParser()
    ap.add_argument("--check", action="store_true")
    ap.add_argument("--preview", type=Path)
    args = ap.parse_args()

    wanted = formulas()
    problems = []
    if not args.check:
        OUT.mkdir(exist_ok=True)
        ensure_meta(OUT, folder=True)
    for name, tex in wanted.items():
        w, h, alpha, png = render(tex)
        data = encode(w, h, alpha)
        target = OUT / f"{name}.bytes"
        if args.preview:
            args.preview.mkdir(parents=True, exist_ok=True)
            (args.preview / f"{name}.png").write_bytes(png)
        if args.check:
            if not target.exists() or target.read_bytes() != data:
                problems.append(f"{target.relative_to(ROOT).as_posix()}: fehlt oder veraltet")
            elif not target.with_name(target.name + ".meta").exists():
                problems.append(f"{target.relative_to(ROOT).as_posix()}: .meta fehlt")
        else:
            target.write_bytes(data)
            ensure_meta(target)
            print(f"{name}: {w} x {h}")
    for stale in sorted(OUT.glob("*.bytes")) if OUT.exists() else []:
        if stale.stem not in wanted:
            if args.check:
                problems.append(f"{stale.relative_to(ROOT).as_posix()}: keine Quelle mehr in ui_*.json")
            else:
                stale.unlink()
                stale.with_name(stale.name + ".meta").unlink(missing_ok=True)
                print(f"{stale.stem}: entfernt")
    for p in problems:
        print(p)
    if problems:
        print("Neu erzeugen: python tools/render_formulas.py")
    return 1 if problems else 0


if __name__ == "__main__":
    sys.exit(main())
