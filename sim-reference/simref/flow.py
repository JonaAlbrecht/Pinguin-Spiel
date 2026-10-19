"""Python-Referenz für MG2 „Polar-Express“: Lattice-Boltzmann D2Q9 mit BGK-Stoß (docs/03, docs/04 Abschnitt 4.3).

Jedes Kästchen trägt neun Anteile Luft f_k, einen je Richtung c_k (ruhend, vier gerade, vier schräg).
Ein Schritt besteht aus zwei Teilen – genau die zwei Teile, die das Spiel auf der Rechentafel zeigt:

1. **Ausgleichen** (Stoß, BGK): In jedem Kästchen nähern sich die Anteile dem Gleichgewicht, das zu Luftmenge und
   Schwung des Kästchens passt:  f_k* = f_k - (f_k - f_k^eq) / tau.  Luftmenge und Schwung bleiben dabei gleich.
2. **Weiterschieben** (Strömen): Jeder Anteil wandert ein Kästchen weit in seine Richtung.
   Ränder: Trifft ein Anteil ein Hindernis (Zug), prallt er zurück (Bounce-Back, halber Abstand).
   Oben und unten ist der Kanal glatt (Spiegelung, kein Haften). Links strömt die Luft ein (Gleichgewicht mit
   Luftmenge 1 und der Einströmgeschwindigkeit, die in ``ramp`` Schritten sanft hochfährt), rechts strömt sie aus
   (Gleichgewicht mit Luftmenge 1 und der Geschwindigkeit der Nachbarspalte – fester Druck; ein bloßes Kopieren der
   Nachbarspalte ließ den Druck im Kanal wandern, die Luft strömte dann um ein Viertel langsamer als eingestellt).
   Ein geschlossener Kasten (Rechentafel) hat links und rechts Wände.

Luftwiderstand per Impulsaustausch: Jeder Anteil, der am Zug zurückprallt, gibt ihm den doppelten Schwung ab.

Die Streaming-Regeln stehen als Quelltabelle (welcher Anteil landet wo) – dieselbe Tabelle baut der C#-Kern
(SFE.SimCore.Flow.LbmD2Q9), damit beide Seiten Schritt für Schritt dasselbe rechnen. Rechenreihenfolge je Kästchen
wie in C# (Summen von links nach rechts, kein numpy.sum), damit nur Rundungsunterschiede ~1e-15 bleiben.
"""
from __future__ import annotations

import math

import numpy as np

# Richtungen: 0 ruhend, 1 rechts, 2 oben, 3 links, 4 unten, 5 rechts oben, 6 links oben, 7 links unten, 8 rechts unten
C = [(0, 0), (1, 0), (0, 1), (-1, 0), (0, -1), (1, 1), (-1, 1), (-1, -1), (1, -1)]
W = [4.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 9.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0, 1.0 / 36.0]
OPP = [0, 3, 4, 1, 2, 7, 8, 5, 6]
MIRROR_Y = [0, 1, 4, 3, 2, 8, 7, 6, 5]
CX = np.array([float(c[0]) for c in C])[:, None]
CY = np.array([float(c[1]) for c in C])[:, None]
WCOL = np.array(W)[:, None]


def equilibrium(rho, ux, uy):
    """Gleichgewicht f_k^eq = w_k rho (1 + 3 c·u + 4,5 (c·u)² - 1,5 u²) – Rechenreihenfolge wie in C#."""
    rho = np.atleast_1d(np.asarray(rho, dtype=float))
    ux = np.atleast_1d(np.asarray(ux, dtype=float))
    uy = np.atleast_1d(np.asarray(uy, dtype=float))
    usq = ux * ux + uy * uy
    cu = CX * ux + CY * uy
    return WCOL * rho * (1.0 + 3.0 * cu + 4.5 * cu * cu - 1.5 * usq)


def moments(f):
    """Luftmenge rho und Geschwindigkeit (ux, uy) je Kästchen – feste Summenreihenfolge."""
    rho = f[0] + f[1] + f[2] + f[3] + f[4] + f[5] + f[6] + f[7] + f[8]
    jx = f[1] - f[3] + f[5] - f[6] - f[7] + f[8]
    jy = f[2] - f[4] + f[5] + f[6] - f[7] - f[8]
    return rho, jx / rho, jy / rho


def inflow_speed(u_in: float, ramp: int, step: int) -> float:
    """Einströmen fährt sanft hoch (halbe Kosinuswelle), sonst läuft eine Druckwelle durch den Kanal."""
    if ramp <= 0 or step >= ramp:
        return u_in
    return u_in * 0.5 * (1.0 - math.cos(math.pi * step / ramp))


class Lbm:
    """D2Q9-BGK auf nx × ny Kästchen (Zelle = y · nx + x, Zeile 0 unten).

    ``u_in`` = Einströmgeschwindigkeit (offener Kanal, startet in Ruhe) oder None (geschlossener Kasten).
    """

    def __init__(self, nx: int, ny: int, tau: float, solid=None, u_in: float | None = None, ramp: int = 0):
        self.nx, self.ny, self.tau, self.u_in, self.ramp = nx, ny, float(tau), u_in, ramp
        n = nx * ny
        self.n = n
        self.solid = np.zeros(n, dtype=bool) if solid is None else np.asarray(solid, dtype=bool).ravel().copy()
        self.solid_idx = np.flatnonzero(self.solid)
        self.iteration = 0
        self.force = (0.0, 0.0)
        self._build_tables()
        self.f = equilibrium(np.ones(n), np.zeros(n), np.zeros(n))
        cols = np.arange(ny) * nx
        self.inlet = cols[~self.solid[cols]]
        outs = cols + nx - 1
        self.outlet = outs[~self.solid[outs]]

    # ---------- Quelltabelle ----------

    def _build_tables(self):
        nx, ny, n = self.nx, self.ny, self.n
        src = np.empty((9, n), dtype=np.int64)
        links_k, links_cell = [], []
        open_ends = self.u_in is not None
        for k in range(9):
            cx, cy = C[k]
            for y in range(ny):
                for x in range(nx):
                    cell = y * nx + x
                    if self.solid[cell]:
                        src[k, cell] = k * n + cell
                        continue
                    sx, sy = x - cx, y - cy
                    if sx < 0 or sx >= nx:
                        # offener Kanal: Platzhalter, Ein-/Ausströmen überschreibt die Spalte; Kasten: Wand
                        src[k, cell] = k * n + cell if open_ends else OPP[k] * n + cell
                    elif sy < 0 or sy >= ny:
                        scell = y * nx + sx
                        if self.solid[scell]:
                            src[k, cell] = OPP[k] * n + cell
                            links_k.append(OPP[k])
                            links_cell.append(cell)
                        else:
                            src[k, cell] = MIRROR_Y[k] * n + scell
                    elif self.solid[sy * nx + sx]:
                        src[k, cell] = OPP[k] * n + cell
                        links_k.append(OPP[k])
                        links_cell.append(cell)
                    else:
                        src[k, cell] = k * n + sy * nx + sx
        self.src = src
        self.links = np.array(links_k, dtype=np.int64) * n + np.array(links_cell, dtype=np.int64)
        self.links_cx = np.array([2.0 * C[k][0] for k in links_k])
        self.links_cy = np.array([2.0 * C[k][1] for k in links_k])

    # ---------- Ein Schritt ----------

    def collide(self, f):
        """Ausgleichen: jedes Kästchen nähert seine Anteile dem Gleichgewicht (Kästchen im Zug bleiben, wie sie sind)."""
        rho, ux, uy = moments(f)
        feq = equilibrium(rho, ux, uy)
        post = f - (f - feq) / self.tau
        if self.solid_idx.size:
            post[:, self.solid_idx] = f[:, self.solid_idx]
        return post

    def step(self, n: int = 1):
        for _ in range(n):
            post = self.collide(self.f)
            flat = post.ravel()
            # Impulsaustausch: Anteil k prallt am Zug zurück und gibt ihm 2 c_k f_k* ab (Summe von vorn nach hinten)
            if self.links.size:
                v = flat[self.links]
                self.force = (float(np.cumsum(self.links_cx * v)[-1]), float(np.cumsum(self.links_cy * v)[-1]))
            self.f = flat[self.src]
            if self.u_in is not None:
                u = inflow_speed(self.u_in, self.ramp, self.iteration + 1)
                self.f[:, self.inlet] = equilibrium(1.0, u, 0.0)
                _, ox, oy = moments(self.f[:, self.outlet - 1])
                self.f[:, self.outlet] = equilibrium(np.ones(self.outlet.size), ox, oy)
            self.iteration += 1

    # ---------- Ausgabe ----------

    def fields(self):
        """rho, ux, uy je Kästchen (Zeile 0 unten); im Zug rho 1 und Geschwindigkeit 0."""
        rho, ux, uy = moments(self.f)
        rho = np.where(self.solid, 1.0, rho)
        ux = np.where(self.solid, 0.0, ux)
        uy = np.where(self.solid, 0.0, uy)
        return rho, ux, uy


# ---------- Rechentafel: eine Reihe Kästchen in einem geschlossenen Kasten ----------

def aggregates(f):
    """Je Kästchen: was nach rechts will, was nach links will, was in der Spalte bleibt."""
    right = f[1] + f[5] + f[8]
    left = f[3] + f[6] + f[7]
    stay = f[0] + f[2] + f[4]
    return right, left, stay


class RowLesson:
    """Fünf Kästchen nebeneinander, links und rechts eine Wand (die Rechentafel in MG2 Schritt 2).

    Dasselbe Modell wie der Kanal (``Lbm`` mit geschlossenen Enden, ny = 1). Je Schritt:
        Luft neu = Luft vorher + kommt an - zieht weiter
    Ausgleichen ändert die Luft im Kästchen nicht, nur ihre Richtung.
    """

    def __init__(self, air, push, tau):
        n = len(air)
        self.lbm = Lbm(n, 1, tau)
        self.lbm.f = equilibrium(np.asarray(air, float), np.asarray(push, float), np.zeros(n))

    @property
    def air(self):
        return moments(self.lbm.f)[0]

    def step(self):
        """Ein Schritt mit Zerlegung für die Tafel: vor/nach dem Ausgleichen, kommt an, zieht weiter."""
        f = self.lbm.f
        before = moments(f)[0]
        r0, l0, s0 = aggregates(f)
        post = self.lbm.collide(f)
        r1, l1, s1 = aggregates(post)
        n = self.lbm.nx
        out = np.zeros(n)
        come = np.zeros(n)
        for x in range(n):
            if x < n - 1:
                out[x] += r1[x]
                come[x + 1] += r1[x]
            if x > 0:
                out[x] += l1[x]
                come[x - 1] += l1[x]
        self.lbm.step()
        after = moments(self.lbm.f)[0]
        return {
            "before": before, "after": after, "comeIn": come, "moveOn": out,
            "rightBefore": r0, "leftBefore": l0, "stayBefore": s0,
            "rightEven": r1, "leftEven": l1, "stayEven": s1,
        }
