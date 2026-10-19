"""Zufallszahlen für MG3 „Fischfabrik“: PCG32 (O'Neill, pcg32_random_r, XSH RR) – bitgenau wie
``SFE.SimCore.Process.Pcg32`` in C#.

Warum PCG32 und nicht ``random``: Python und C# müssen dieselbe Zufallsfolge ziehen, sonst laufen die Golden Files
(Ereignisse einer Schicht) auseinander. PCG32 ist klein, schnell und hat eigene Ströme: Jeder Zweck (Kutter, Möwen,
jede Station, jede Maschine) bekommt einen eigenen Strom. Ändert der Spieler die Zahl der Maschinen, kommen die Kutter
trotzdem zu denselben Zeiten – „gleicher Tag, anderer Plan“ (gemeinsame Zufallszahlen).

Verteilungen über die Umkehrfunktion, mit genau einer Zufallszahl je Wert. Nur ``log`` und ``sqrt`` – keine
Winkelfunktionen, deren letzte Stelle sich zwischen Laufzeitbibliotheken unterscheiden könnte (darum keine
Normalverteilung, siehe Spec 005).
"""
from __future__ import annotations

import math

MASK64 = (1 << 64) - 1
MULTIPLIER = 6364136223846793005

# Feste Strom-Nummern je Zweck (dieselben in C#)
STREAM_KUTTER = 1
STREAM_GULLS = 2
STREAM_SERVICE = 10      # + Stufe
STREAM_FAILURES = 100    # + 10 · Stufe + Maschine


class Pcg32:
    def __init__(self, seed: int, stream: int):
        self.state = 0
        self.inc = ((stream << 1) | 1) & MASK64
        self.next_uint()
        self.state = (self.state + seed) & MASK64
        self.next_uint()

    def next_uint(self) -> int:
        old = self.state
        self.state = (old * MULTIPLIER + self.inc) & MASK64
        xorshifted = (((old >> 18) ^ old) >> 27) & 0xFFFFFFFF
        rot = old >> 59
        return ((xorshifted >> rot) | (xorshifted << ((-rot) & 31))) & 0xFFFFFFFF

    def next_double(self) -> float:
        """Gleichverteilt in [0, 1) mit 32 Bit Auflösung."""
        return self.next_uint() / 4294967296.0


def exponential(rng: Pcg32, mean: float) -> float:
    """Wartezeit bis zum nächsten zufälligen Ereignis (Kutter, Störung, Reparatur)."""
    return -mean * math.log(1.0 - rng.next_double())


def triangular(rng: Pcg32, low: float, mode: float, high: float) -> float:
    """Arbeitszeit einer Station: meist ``mode``, nie unter ``low``, selten bis ``high``."""
    u = rng.next_double()
    f = (mode - low) / (high - low)
    if u < f:
        return low + math.sqrt(u * (high - low) * (mode - low))
    return high - math.sqrt((1.0 - u) * (high - low) * (high - mode))
