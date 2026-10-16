"""Python-Referenz für MG1 Teil A „Der letzte Huddle“ (transientes Wärmenetzwerk, implizites Euler).

Jeder Pinguin ist ein Knoten i mit Temperatur T_i:

    C dT_i/dt = q - h_i (T_i - T_luft) + sum_j G (T_j - T_i)      (j = Pinguine in Kontaktabstand)

h_i = hExposed, wenn kein Pinguin im Luv (windaufwärts) innerhalb shieldDistance steht, sonst hShielded.
Implizites Euler:  (C/dt + h_i + sum_j G) T_i' - sum_j G T_j' = C/dt T_i + q + h_i T_luft
"""
from __future__ import annotations

import json
import math
from dataclasses import dataclass
from pathlib import Path

import numpy as np
import scipy.sparse as sp
import scipy.sparse.linalg as spla


@dataclass
class HuddleParams:
    heat: float
    capacity: float
    contact: float
    h_exposed: float
    h_shielded: float
    contact_distance: float
    shield_distance: float
    shield_cone: float
    air_temp: float


def load_params(path: Path) -> HuddleParams:
    d = json.loads(Path(path).read_text(encoding="utf-8"))["model"]
    return HuddleParams(d["heat"], d["capacity"], d["contact"], d["hExposed"], d["hShielded"],
                        d["contactDistance"], d["shieldDistance"], d["shieldCone"], d["airTemp"])


def exposure(pos: np.ndarray, wind_to: tuple[float, float], p: HuddleParams) -> np.ndarray:
    """True = Pinguin steht ungeschützt im Wind."""
    wx, wy = wind_to
    n = math.hypot(wx, wy)
    wx, wy = wx / n, wy / n
    exposed = np.ones(len(pos), dtype=bool)
    for i in range(len(pos)):
        for j in range(len(pos)):
            if i == j:
                continue
            vx, vy = pos[j, 0] - pos[i, 0], pos[j, 1] - pos[i, 1]
            d = math.hypot(vx, vy)
            if d == 0.0 or d > p.shield_distance:
                continue
            # j steht im Luv von i, wenn der Vektor i->j gegen die Windrichtung zeigt
            if -(vx * wx + vy * wy) / d >= p.shield_cone:
                exposed[i] = False
                break
    return exposed


def step(temps: np.ndarray, pos: np.ndarray, wind_to: tuple[float, float], p: HuddleParams,
         dt: float) -> np.ndarray:
    n = len(temps)
    exp = exposure(pos, wind_to, p)
    h = np.where(exp, p.h_exposed, p.h_shielded)
    c = p.capacity / dt
    rows, cols, vals = [], [], []
    diag = c + h
    for i in range(n):
        for j in range(i + 1, n):
            if math.dist(pos[i], pos[j]) <= p.contact_distance:
                diag[i] += p.contact
                diag[j] += p.contact
                rows += [i, j]
                cols += [j, i]
                vals += [-p.contact, -p.contact]
    rows += list(range(n))
    cols += list(range(n))
    vals += list(diag)
    a = sp.csr_matrix((vals, (rows, cols)), shape=(n, n))
    b = c * temps + p.heat + h * p.air_temp
    return spla.spsolve(a.tocsc(), b)
