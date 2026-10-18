"""Python-Referenz für das Zwei-Felder-Beispiel in MG1 „Die erste Siedlung“ (Ende von Schritt 2).

Zwei Zellen der Siedlung, 0 = Laterne, 1 = Iglu, mit derselben Zellbilanz wie das Raster – nur mit
Speicherglied, damit man das Rechnen Zeitschritt für Zeitschritt sieht:

    C dT_i/dt = q_i - h (T_i - T_luft) + k (T_j - T_i)

Implizites Euler:  (C/dt + h + k) T_i' - k T_j' = C/dt T_i + q_i + h T_luft
Der Endzustand (dT/dt = 0) ist genau die stationäre Bilanz der Siedlung für diese zwei Zellen.
"""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np


@dataclass
class PairLesson:
    lantern_heat: float
    igloo_heat: float
    k: float
    h: float
    capacity: float
    air_temp: float
    dt: float
    steps: int

    @property
    def heat(self) -> np.ndarray:
        return np.array([self.lantern_heat, self.igloo_heat])


def load(path: Path) -> PairLesson:
    """Werte aus dem Siedlungs-Level: Block ``pairLesson``, Lufttemperatur aus ``climate``."""
    d = json.loads(Path(path).read_text(encoding="utf-8"))
    p = d["pairLesson"]
    return PairLesson(p["lanternHeat"], p["iglooHeat"], p["k"], p["h"], p["capacity"],
                      d["climate"]["airTemp"], p["dt"], p["steps"])


def start(p: PairLesson) -> np.ndarray:
    """Beide Felder starten bei Lufttemperatur – die Laterne wird gerade angeschaltet."""
    return np.full(2, p.air_temp)


def step(temps: np.ndarray, p: PairLesson) -> np.ndarray:
    c = p.capacity / p.dt
    a = np.array([[c + p.h + p.k, -p.k], [-p.k, c + p.h + p.k]])
    b = c * temps + p.heat + p.h * p.air_temp
    return np.linalg.solve(a, b)


def steady(p: PairLesson) -> np.ndarray:
    """Gleichgewichtspunkt: dieselbe Bilanz ohne Speicherglied (wie ``settlement.solve``)."""
    a = np.array([[p.h + p.k, -p.k], [-p.k, p.h + p.k]])
    return np.linalg.solve(a, p.heat + p.h * p.air_temp)


@dataclass
class StepTerms:
    """Zerlegung eines Zeitschritts je Feld – die Rechentafel im Spiel.

        neu - vorher = own + from_neighbour - to_wind

    Beim impliziten Euler gelten die Teile mit den NEUEN Temperaturen; dann geht die Zerlegung exakt auf.
    from_neighbour summiert zu null: Der Austausch erzeugt und vernichtet keine Wärme.
    """
    own: np.ndarray
    from_neighbour: np.ndarray
    to_wind: np.ndarray


def step_terms(after: np.ndarray, p: PairLesson) -> StepTerms:
    f = p.dt / p.capacity
    flow = f * p.k * (after[1] - after[0])          # > 0: Wärme fließt vom Iglu zur Laterne
    return StepTerms(f * p.heat, np.array([flow, -flow]), f * p.h * (after - p.air_temp))
