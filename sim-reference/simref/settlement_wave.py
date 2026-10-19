"""Python-Referenz für die Wärmewelle in MG1 Schritt 3 „Alles hängt zusammen“.

Dieselbe Bilanz wie die Siedlung (``settlement.assemble``), nur mit Wärmespeicher C je Feld, damit man sieht,
wie die Wärme Zeitschritt für Zeitschritt von Feld zu Feld wandert:

    C dT/dt = b - A T          (A, b aus settlement.assemble)

Implizites Euler:  (C/dt I + A) T' = C/dt T + b
Der Endzustand (dT/dt = 0) ist genau die stationäre Lösung ``settlement.solve`` – der Gleichgewichtspunkt.
"""
from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import scipy.sparse as sp
import scipy.sparse.linalg as spla

from simref import settlement


@dataclass
class WaveLesson:
    capacity: float
    dt: float
    steps: int
    manual_steps: int
    tiles: list[str]


def load(path: Path) -> WaveLesson:
    w = json.loads(Path(path).read_text(encoding="utf-8"))["wave"]
    return WaveLesson(w["capacity"], w["dt"], w["steps"], w["manualSteps"], w["tiles"])


def run(lesson: WaveLesson, level: settlement.Level, mats: settlement.MaterialSet) -> list[np.ndarray]:
    """Temperaturen nach jedem Zeitschritt (je ein Raster), Start bei Lufttemperatur."""
    a, b = settlement.assemble(lesson.tiles, level, mats)
    n = a.shape[0]
    c = lesson.capacity / lesson.dt
    system = (a + sp.identity(n) * c).tocsc()
    t = np.full(n, level.air_temp)
    h, w = len(lesson.tiles), len(lesson.tiles[0])
    history = []
    for _ in range(lesson.steps):
        t = spla.spsolve(system, c * t + b)
        history.append(t.reshape(h, w))
    return history
