"""Gezielt gebaute Prüf-Siedlungen, die Tests und Golden Files gemeinsam nutzen.

Jede deckt eine Regel ab, die die übrigen Golden Files nicht treffen (Review zu PR #2:
mit nur einem Schmelz-Durchlauf je Fall blieb die Kaskade ungeprüft).
"""
from __future__ import annotations


def _grid(rows: dict[int, str]) -> list[str]:
    return [rows.get(y, "." * 24) for y in range(24)]


# Zwei Iglus schmelzen, dann eines mehr: Wasser leitet besser als Iglu und reicht die Laternenwärme weiter.
MELT_TWO_PASSES = _grid({
    2: "...............I........",
    3: "............I...........",
    4: "........L..I............",
    5: "..........IL............",
    6: ".........L.L............",
    7: "..........LL..L.........",
    8: "...........L..L.........",
    9: ".........L..L...........",
})

# Ein einzelner Schneewall in der Mitte – für den Windschatten in allen acht Richtungen.
SINGLE_WALL = _grid({12: "............W..........."})
