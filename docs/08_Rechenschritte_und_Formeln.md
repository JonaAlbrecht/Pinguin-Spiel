# 08 – Rechenschritte und Formeln für alle Minigames (Plan)

> Status: **Plan, nichts davon ist gebaut** (Stand 18.10.2026). Vorbild ist die Rechentafel „Zwei Felder: Laterne und Iglu“
> in MG1 Schritt 2 (ADR-12, ADR-18, ADR-20). Zielgruppe dieses Dokuments: Entwicklerteam und Product Owner.
> Was Spielende sehen, bleibt formelfrei (docs/00). Formeln erscheinen nur unter „Blick unter die Haube“.
> Alle Beispielzahlen unten sind nachgerechnet. Die Rechnungen gehören beim Bauen nach `sim-reference/simref/`.

## 1. Worum es geht

In MG1 sieht der Spieler einmal ganz klein, was der Computer später für fast 600 Felder auf einmal tut: zwei Felder,
eine Rechentafel, ein Klick je Schritt. Dieses Dokument plant dasselbe für jedes weitere Minigame:

- **den Rechenschritt**: das kleinste Beispiel aus dem echten Modell des Minigames, Schritt für Schritt durchklickbar,
- **die Diskretisierung**: wie aus dem stetigen Naturgesetz etwas wird, das ein Computer in Schritten rechnen kann,
- **die Formeln** für „Blick unter die Haube“: Gesetz, diskreter Schritt, Werte des Beispiels.

Jedes Minigame zeigt dabei eine **andere Art** der Diskretisierung. Wer alle spielt, hat die wichtigsten Grundideen
der Simulation einmal selbst durchgeklickt. Das ist ein Lernziel für sich.

| Minigame | Art der Diskretisierung | Kleinstes Beispiel | Knopf | Was erhalten bleibt (Bilanzzeile) | Aha |
|---|---|---|---|---|---|
| MG1 Siedlung *(gebaut)* | Zeit (implizites Euler) + Raum (Finite Volumen) | Laterne und Iglu | Nächster Zeitschritt | Wärme | Gleichgewichtspunkt |
| MG01 Hitzeinsel | wie MG1, plus Quelle und Senke je Kachel | Asphalt und Baum | Nächster Zeitschritt | Wärme | Baum kühlt doppelt: Schatten und Verdunstung |
| MG2 Schlitten | Gitter-Boltzmann: Luft als Päckchen auf einem Gitter | 4 Luftkästchen vor der Zugnase | Mischen und weiterschieben | Luft | Vor einer flachen Nase staut sich Luft und drückt |
| MG2-B Fischfang | wie MG2, Temperatur reist mit | dieselben 4 Kästchen | wie MG2 | Luft und Wärme | Kühle Luft muss erst hinkommen |
| MG02 Windschneise | Raum (Gitter), Gleichgewicht durch Ausgleichsrunden | eine Gasse quer, 4 Kästchen | Nächste Ausgleichsrunde | Luftmenge durch die Gasse | Enge Gasse: doppelt so schnell |
| MG03 Brücke | Bauwerk in Balken und Knoten (Finite Elemente), Last in Positionen | zwei Balken, ein Knoten | Nachgeben | Kräfte am Knoten | Nachgeben, bis sich die Kräfte aufheben |
| MG3 / MG04 Fischfabrik | Ereignisse statt fester Zeitschritte | 5 Kisten, 1 Station | Nächstes Ereignis | Kisten | Die Uhr springt, und Unregelmäßigkeit allein macht Stau |
| MG05 Pinguin-Express | Zeit (Euler, 1 Sekunde) | zwei Züge auf einer Strecke | Nächste Sekunde | Weg = Tempo × Zeit | Doppelt so schnell heißt vierfacher Bremsweg |
| MG06 Netz-Balance | Netz als Knoten und Leitungen, Tag in Stunden | Umspannwerk, Solarhaus, Suppenhaus mit Akku | Nächste Stunde | Strom | Der Akku verschiebt die Mittagssonne in den Abend |
| MG07 Hitzewelle | Kopplungsschritte zwischen Modellen | eine Stunde Hitze durch drei Modelle | Nächster Kopplungsschritt | je Modell | Alles hängt zusammen |

MG00 Huddle ist seit ADR-20 entfallen. Die Zwei-Pinguine-Tafel ging in der Tafel von MG1 auf.

## 2. Gemeinsames Muster

Jeder Rechenschritt folgt denselben sieben Regeln, damit Spieler das Muster wiedererkennen und wir eine einzige
Komponente bauen statt zehn.

1. **Kleinstes Beispiel aus dem echten Modell.** Dieselben Gleichungen wie die große Simulation, nur 2 bis 5 Elemente,
   eigene, lesbare Werte im Level-JSON (Block `stepLesson`, wie `pairLesson` in MG1). Keine zweite Lehr-Wahrheit
   (verworfen in ADR-12).
2. **Platz im Phasen-Gerüst:** am Ende von Phase 2 „Welche Regel gilt?“. Die Waage bekommt Zahlen. Abweichungen sind
   unten begründet: Brücke in Phase 4, Fischfabrik in Phase 4 vor der Möwen-Frage.
3. **Rechentafel = Bild-Gleichung.** Spalten in Alltagssprache, gleiche Farben wie die Symbole auf der Waage,
   Zeilen = Elemente des Beispiels. Ein Klick füllt eine Zeile von links nach rechts, erst die Teile, dann das Ergebnis.
4. **Bilanzzeile:** Unter der Tafel steht nach jedem Schritt, was erhalten bleibt („Beim Weitergeben geht keine Wärme
   verloren“). Das ist zugleich der wichtigste Test (siehe Abschnitt 4).
5. **Ein Aha-Satz** nach dem ersten und nach dem letzten Schritt, der zur Schwerpunkt-Phase des Minigames überleitet.
6. **Blick unter die Haube (Pop-up, ADR-18):** drei Blöcke wie in MG1. Erstens das Gesetz, stetig. Zweitens der
   diskrete Schritt, den die Tafel zeigt. Drittens Kästchen der Bild-Gleichung je Term und die Werte des Beispiels.
   LaTeX-Quellen in `ui_de.json` unter `underTheHood.formulas`, Schlüssel mit Minigame-Präfix (`mg2_lbm_step`, …).
7. **Sprache:** Spieltexte bestehen den Text-Lint. Also keine Einheiten wie „mm“, „s“, „kW“ und keine Fachwörter
   wie „Iteration“, „Matrix“ oder „Parameter“. Wörter dafür: „Ausgleichsrunde“, „Schritt“, „Wert“. Zahlen ohne Einheit,
   die Einheit steht nur unter der Haube.

## 3. Die Minigames im Einzelnen

Notation unter der Haube: $n$ zählt Schritte, $\Delta t$ ist der Zeitschritt, $i, j$ sind Nachbarn.

### MG01 – Hitzeinsel: „Zwei Kacheln: Asphalt und Baum“

**Tafel:** *vorher + Sonne − Verdunstung + vom Nachbarn − an die Luft = neu*, eine Zeile je Kachel. Start morgens,
beide Kacheln so warm wie die Luft.

**Gesetz** (dieselbe Zellbilanz wie MG1, die Laterne wird zur Sonne):

$$C\,\frac{\mathrm{d}T_i}{\mathrm{d}t} = \alpha_i S - e_i + k\,(T_j - T_i) - h\,(T_i - T_{\mathrm{Luft}})$$

**Diskretisierung:** implizites Euler wie in MG1, das Raster als Finite Volumen (32 × 32, 4 m):

$$\Big(\tfrac{C}{\Delta t} + h + k\Big)T_i^{n+1} - k\,T_j^{n+1} = \tfrac{C}{\Delta t}T_i^{n} + \alpha_i S - e_i + h\,T_{\mathrm{Luft}}$$

Die große Simulation rechnet nur den Endzustand ($\mathrm{d}T/\mathrm{d}t = 0$, $A\,T = b$, CG-Löser).

**Beispiel** ($S = 10$, $\alpha$ = 0,9 Asphalt und 0,4 Baum im Schatten, $e$ = 0 und 3, $k = 1$, $h = 0{,}5$, $C = 10$,
$\Delta t = 10$, $T_{\mathrm{Luft}} = 22$), nachgerechnet:

| Schritt | Asphalt | Baum |
|---|---|---|
| 0 | 22,0 | 22,0 |
| 1 | 22,0 + 9,0 − 0 − 2,3 − 2,2 = **26,5** | 22,0 + 4,0 − 3,0 + 2,3 − 1,1 = **24,2** |
| 6 | **32,7** | **29,5** |
| Gleichgewicht | 33,6 | 30,4 |

**Aha:** „Der Baum kühlt doppelt: Er wirft Schatten und schwitzt. Und der Asphalt heizt seinen Nachbarn mit.“
Das leitet zu Phase 3 über: Wärmefäden zwischen allen Kacheln.

**Umsetzung:** Das ist dieselbe Komponente wie MG1 mit zwei zusätzlichen Spalten. Sie ist der beste erste Kandidat,
um `PairLessonView` zur allgemeinen Rechentafel zu machen (Abschnitt 4).

### MG2 – Schlitten: „Luftpäckchen vor der Zugnase“

Der Rechenschritt ist hier ein **anderer** als in MG1: Die Luft wird nicht als Temperaturfeld gerechnet, sondern als
viele kleine Päckchen, die sich in jedem Schritt erst **mischen** und dann ins Nachbarkästchen **weiterhüpfen**
(Gitter-Boltzmann).

**Tafel:** vier Luftkästchen in einer Reihe, links kommt Gegenwind herein, rechts ist die flache Zugnase. Je Kästchen
gibt es drei Päckchen: nach links, bleibt, nach rechts. Spalten: *Luft vorher | nach dem Mischen | kommt von links |
kommt von rechts | prallt ab = Luft neu*, dazu *Tempo*. Die Zahlen ×100, damit sie ganz bleiben (Luft 100 = ruhige Luft).

**Gesetz** (Strömung, vereinfacht; im Spiel ehrlich als niedrige Geschwindigkeit benannt):

$$\frac{\partial \mathbf{u}}{\partial t} + (\mathbf{u}\cdot\nabla)\mathbf{u} = -\frac{\nabla p}{\rho} + \nu\,\nabla^2\mathbf{u}, \qquad \nabla\cdot\mathbf{u} = 0$$

Gitter-Boltzmann rechnet stattdessen mit Verteilungen $f_i$ der Päckchen je Richtung $\mathbf{c}_i$:

$$\frac{\partial f_i}{\partial t} + \mathbf{c}_i\cdot\nabla f_i = -\frac{1}{\tau}\big(f_i - f_i^{\mathrm{eq}}\big)$$

**Diskretisierung** (D2Q9 im Spiel, D1Q3 auf der Tafel): Mischen (Kollision), dann Weiterschieben (Strömung):

$$f_i(\mathbf{x} + \mathbf{c}_i\Delta t,\ t + \Delta t) = f_i(\mathbf{x}, t) - \frac{1}{\tau}\big(f_i - f_i^{\mathrm{eq}}\big)$$

$$f_i^{\mathrm{eq}} = w_i\,\rho\Big(1 + \frac{\mathbf{c}_i\cdot\mathbf{u}}{c_s^2} + \frac{(\mathbf{c}_i\cdot\mathbf{u})^2}{2c_s^4} - \frac{\mathbf{u}^2}{2c_s^2}\Big),\quad \rho = \sum_i f_i,\quad \rho\mathbf{u} = \sum_i \mathbf{c}_i f_i,\quad \nu = c_s^2\big(\tau - \tfrac12\big)\Delta t$$

An der Zugnase prallen Päckchen zurück. Jedes abprallende Päckchen schiebt die Nase ein Stück. Daraus kommt der Luftwiderstand:

$$f_{\bar\imath}(\mathbf{x}_b, t + \Delta t) = f_i^{*}(\mathbf{x}_b, t), \qquad \mathbf{F} = \sum_{\text{Wand}} 2\,\mathbf{c}_i f_i^{*}$$

Fahrzeit (1D-Modell aus doc 03) bei Antriebsleistung $P$: $\;P = \tfrac12\rho\,c_w A\,v^{3}\Rightarrow v^{*} = \big(2P/(\rho c_w A)\big)^{1/3}$, Fahrzeit $= s/v^{*}$.

**Beispiel** (D1Q3, $w = (\tfrac16, \tfrac46, \tfrac16)$, $c_s^2 = \tfrac13$, $\tau = 1$, also „Mischen“ = ganz ins
Gleichgewicht, Einstrom $\rho = 1$, $u = 0{,}1$), nachgerechnet, ×100:

| Schritt | Luft in Kästchen 1 … 4 | Druck auf die Nase |
|---|---|---|
| 1 | 105 · 100 · 100 · 100 | 33 |
| 4 | 111 · 109 · 105 · 102 | 33 |
| 8 | 111 · 112 · 114 · 116 | 39 |

**Bilanzzeile:** „Luft verschwindet nicht: Was links hereinkommt, minus was links wieder hinausgeht, ist jetzt in den Kästchen.“
**Aha:** „Vor der flachen Nase staut sich die Luft und drückt immer stärker, das ist Kalles Problem.“ Danach Phase 3
(grob, mittel, fein): Mehr Kästchen heißt mehr Päckchen und mehr Schritte, also mehr Rechenzeit.

**Offen:** 1D zeigt den Stau, aber nicht das „Herum-Strömen“. Alternative: ein 3 × 3-Ausschnitt in D2Q9 als
Bild ohne Zahlen. Mein Vorschlag: die 1D-Tafel mit Zahlen und danach eine kurze Animation in 2D ohne Zahlen.

### MG2-B – Fischfang (Bonus): „Kühle Luft reist mit“

Gleiche Tafel wie MG2. Jedes Päckchen trägt zusätzlich eine Temperatur (zweite Verteilung $g_i$):

$$\frac{\partial T}{\partial t} + \mathbf{u}\cdot\nabla T = \kappa\,\nabla^2 T \quad\Rightarrow\quad g_i(\mathbf{x} + \mathbf{c}_i\Delta t, t + \Delta t) = g_i - \frac{1}{\tau_T}\big(g_i - w_i T (1 + \tfrac{\mathbf{c}_i\cdot\mathbf{u}}{c_s^2})\big),\quad \kappa = c_s^2\big(\tau_T - \tfrac12\big)$$

Bilanzzeile: Luft *und* Wärme. Aha: „Ein Lüfter hilft nur, wenn die kühle Luft auch am heißen Bauteil ankommt.“
Erst bauen, wenn MG2-B nach Release 1 entschieden ist.

### MG02 – Windschneise: „Ausgleichsrunden in der Gasse“

**Tafel:** eine Gasse quer zwischen zwei Häusern, 5 Punkte von Hauswand zu Hauswand. An den Wänden steht fest, wie
viel Luft „links“ und „rechts“ vorbeiströmt (0 und 1). Jede **Ausgleichsrunde** nimmt jeder innere Punkt den
Mittelwert seiner Nachbarn. Spalten: *links | rechts | Mittelwert = neu*, darunter das Tempo zwischen zwei Punkten.

**Gesetz** (wirbelfreie, inkompressible Strömung, im Spiel ehrlich als Vereinfachung benannt):

$$u = \frac{\partial\psi}{\partial y},\quad v = -\frac{\partial\psi}{\partial x},\qquad \nabla^2\psi = 0,\qquad \psi = \text{konstant an jeder Hauswand}$$

**Diskretisierung:** Gitter mit Kästchen (im Spiel Quadtree, der Spieler malt die Feinheit), 5-Punkt-Stern:

$$\psi_{i,j} = \tfrac14\big(\psi_{i+1,j} + \psi_{i-1,j} + \psi_{i,j+1} + \psi_{i,j-1}\big),\qquad u \approx \frac{\psi_{j+1} - \psi_j}{\Delta y}$$

Die Tafel rechnet diese Gleichung als Ausgleichsrunden (Jacobi). Das Spiel löst dieselbe Gleichung mit dem CG-Löser,
mit gleichem Ergebnis und schneller. Unter der Haube steht das ausdrücklich.

**Beispiel** (nachgerechnet): Gasse 4 Kästchen breit, nach 6 Runden 0 · 0,19 · 0,44 · 0,69 · 1, am Ende
0 · 0,25 · 0,50 · 0,75 · 1, Tempo überall 0,25. Dieselbe Luftmenge durch eine Gasse von 2 Kästchen: Tempo 0,5.

**Bilanzzeile:** „Durch die Gasse geht immer gleich viel Luft (rechts minus links = 1).“
**Aha:** „Enge Gasse, doppelt so schnell, wie beim Gartenschlauch. Und mehr Kästchen brauchen mehr Runden.“
Das ist die Brücke zum Schwerpunkt „Genau hinschauen kostet Zeit“.

### MG03 – Brücke: „Zwei Balken, ein Knoten“

**Platz:** in Phase 4 (Schwerpunkt „Ergebnis verstehen“), vor den Detektiv-Fragen. In Phase 2 steht schon die Feder als Bild.

**Tafel:** ein Bock aus zwei schrägen Balken, der Zug drückt auf den Knoten oben. Je Klick gibt der Knoten etwas nach,
die Balken drücken stärker zurück, bis sich die Kräfte aufheben. Spalten: *Last | Balken drücken zurück | Rest |
Knoten sinkt*. Danach je Balken: *Kraft | gedrückt oder gezogen (blau/rot) | Ampel*.

**Gesetz:**

$$N = \frac{EA}{L}\,\Delta L,\qquad \sigma = \frac{N}{A},\qquad \sum \mathbf{F} = 0 \text{ an jedem Knoten},\qquad F_{\mathrm{knick}} = \frac{\pi^2 E I}{L^2}$$

**Diskretisierung:** Das Bauwerk wird in Stäbe und Knoten zerlegt (Fachwerk-FEM). Jeder Stab trägt seine Steifigkeit
bei, alle zusammen ergeben $K\,u = f$. Die fahrende Last wird in 5 Positionen zerlegt (quasistatische Lastschritte):

$$k_e = \frac{EA}{L}\begin{pmatrix} c^2 & cs & -c^2 & -cs\\ cs & s^2 & -cs & -s^2\\ -c^2 & -cs & c^2 & cs\\ -cs & -s^2 & cs & s^2 \end{pmatrix},\qquad K = \sum_e k_e,\qquad K\,u = f$$

Die Tafel zeigt das Nachgeben als gedämpfte Ausgleichsrunden $u^{m+1} = u^m + \omega\,(f - K u^m)/K_{ii}$. Das Spiel
löst direkt (Cholesky) mit gleichem Ergebnis.

**Beispiel** (Stahl $E = 200$ GPa, $A = 10$ cm², Balken 2,5 m, Höhe 1,5 m, Last 50 kN, $\omega = 0{,}6$), nachgerechnet:
Balken drücken zurück 30 → 42 → 46,8 → 48,7 → 49,5 (Ziel 50); Knoten sinkt 0,52 → 0,73 → 0,81 → 0,85 → 0,86 (Ende 0,87 mm);
je Balken 41,7 kN gedrückt, Spannung 42 MPa (Grenze Baustahl 235 MPa, Knicken prüfen). Auf der Tafel stehen die Werte
ohne Einheiten, die Durchbiegung übertrieben gezeichnet (wie in doc 05).

**Bilanzzeile:** „Am Knoten heben sich die Kräfte auf: Last = was die Balken zurückdrücken.“
**Aha:** „Die Brücke gibt nach, bis sie dagegenhält. Genau diesen Punkt sucht der Computer für jeden Knoten.“
Das ist dieselbe Idee wie der Gleichgewichtspunkt in MG1, und ZWILLI kann darauf zurückverweisen.

### MG3 / MG04 – Fischfabrik / Smart Harbor: „Die Uhr springt“

**Platz:** in Phase 4, vor Phase 5 „Die Möwen-Frage“. Gleiche Tafel für MG3 und MG04 (ein Kern, siehe doc 03).

**Tafel:** eine Ereignisliste. Spalten: *Uhr | was passiert | Kisten in der Schlange | Station frei/belegt |
Wartezeit der Kiste*. „Nächstes Ereignis“ springt die Uhr direkt zum nächsten Eintrag. Dazwischen passiert nichts,
also wird auch nichts gerechnet.

**Gesetz** (Warteschlangen statt Differentialgleichung):

$$W_{n+1} = \max\big(0,\ W_n + S_n - A_{n+1}\big)\ \ \text{(Lindley)},\qquad L = \lambda W\ \ \text{(Little)},\qquad \rho = \frac{\lambda}{\mu} < 1$$

Zufall: Ankunftsabstände $A = -\ln(U)/\lambda$ mit $U$ gleichverteilt (PCG32, fester Seed), Bearbeitung dreieck- oder
normalverteilt, Störungen nach MTBF/MTTR. Über 10 Tage: $\bar x \pm t_{0{,}95;\,9}\, s/\sqrt{10}$.

**Diskretisierung:** Zeit wird nicht in gleiche Schritte geteilt, sondern in **Ereignisse** (Next-Event-Time-Advance):

$$t \leftarrow \min\{\,t_e : e \in \text{Ereignisliste}\,\},\qquad \text{Zustand ändert sich nur bei } t_e$$

**Beispiel** (Kutter bringen Kisten bei 0, 2, 3, 7, 8 Minuten, Filetieren dauert je 2 Minuten), nachgerechnet:
Wartezeiten 0 · 0 · 1 · 0 · 1, im Mittel 0,4. Kommen dieselben 5 Kisten gleichmäßig (0, 2, 4, 6, 8), wartet keine.

**Bilanzzeile:** „Angekommen = fertig + in Arbeit + wartend. Keine Kiste geht verloren (außer an die Möwen).“
**Aha:** „Die Station schafft im Schnitt genug, trotzdem wartet Fisch, nur weil die Kutter unregelmäßig kommen.“
Das führt direkt zur Möwen-Frage: Ein Tag reicht nicht, man muss viele Tage simulieren.

**Test zusätzlich:** Gegen die Formel für M/M/1 ($W_q = \rho/(\mu - \lambda)$) über viele Ankünfte prüfen, ob die
Ereignisliste richtig rechnet.

### MG05 – Pinguin-Express: „Zwei Züge, Sekunde für Sekunde“

**Tafel:** zwei Züge, Zug 2 steht im Bahnhof, Zug 1 kommt mit Tempo 10 an. Je Zug: *Ort vorher + Weg in dieser
Sekunde = Ort neu*, *Tempo vorher ± Gas oder Bremse = Tempo neu*, dazu *Abstand* und *braucht Abstand*.

**Gesetz:**

$$\frac{\mathrm{d}x}{\mathrm{d}t} = v,\qquad m\,\frac{\mathrm{d}v}{\mathrm{d}t} = F_{\mathrm{Antrieb}} - F_{\mathrm{Bremse}} - F_W(v),\qquad s_{\mathrm{Brems}} = \frac{v^2}{2b}$$

Mitfahrender Abstand (Moving Block): $d = v\,t_R + \dfrac{v^2}{2b} + d_0$. Festblock: Der Folgezug darf erst in einen
Block, wenn der nächste frei ist. Energie: $E = \sum F_{\mathrm{Antrieb}}\,v\,\Delta t$.

**Diskretisierung:** feste Zeitschritte $\Delta t = 1$ s, symplektisches Euler (erst Tempo, dann Ort):

$$v^{n+1} = v^n + a^n\,\Delta t,\qquad x^{n+1} = x^n + v^{n+1}\,\Delta t$$

**Beispiel** ($b = 1$, $t_R = 1$, $d_0 = 10$), nachgerechnet: Abstand 60, gebraucht 70, also bremst Zug 1. Tempo
9, 8, 7, …; Abstand 51, 43, 36, …; nach 8 Sekunden ist der Abstand (18) wieder größer als nötig (17,5).

**Bilanzzeile:** „Weg = Tempo × Zeit, Sekunde für Sekunde addiert.“
**Aha:** „Doppelt so schnell heißt viermal so langer Bremsweg.“ Danach Upgrade „Intelligente Zugsteuerung“: Der
Abstand muss nur so groß sein wie nötig, nicht ganze Blöcke, also passen mehr Züge auf die Strecke.

### MG06 – Netz-Balance: „Ein Tag in drei Stunden“

**Tafel:** Umspannwerk, ein Haus mit Solardach, ein Haus mit Suppenkocher und Akku. Je Stunde (morgens, mittags,
abends): *Sonne + aus dem Akku − verbraucht = über die Leitung*, daneben die Ampel der Leitung. Erst ohne Akku, dann mit.

**Gesetz** (Gleichstrom-Lastfluss, Kirchhoff an jedem Knoten):

$$P_{ij} = \frac{\theta_i - \theta_j}{x_{ij}},\qquad \sum_j P_{ij} = P_i \;\Rightarrow\; B\,\theta = P,\qquad |P_{ij}| \le P_{ij}^{\max}$$

**Diskretisierung:** Das Netz ist schon diskret (Knoten und Leitungen). Der Tag wird in 24 Stunden zerlegt, jede
Stunde für sich gerechnet (quasistationär). Nur der Akku verbindet die Stunden:

$$E^{n+1} = E^{n} + \eta\,P_{\mathrm{laden}}\,\Delta t - \frac{P_{\mathrm{entladen}}}{\eta}\,\Delta t,\qquad 0 \le E \le E_{\max}$$

**ZWILLI-Aha unter der Haube**, beide Gleichungen nebeneinander:

$$\sum_j k_{ij}\,(T_j - T_i) = -q_i \qquad\longleftrightarrow\qquad \sum_j B_{ij}\,(\theta_j - \theta_i) = -P_i$$

Gleicher Aufbau, gleicher Löser wie MG1 und MG01.

**Beispiel** (Leitung trägt 4, Akku fasst 4), nachgerechnet:

| Stunde | Sonne | verbraucht | ohne Akku: Leitung | mit Akku: Akku | mit Akku: Leitung |
|---|---|---|---|---|---|
| morgens | 1 | 2 | 1 (grün) | 0 | 1 (grün) |
| mittags | 6 | 1 | −5 zurück (rot) | lädt 4 | −1 (grün) |
| abends | 0 | 5 | 5 (rot) | gibt 4 | 1 (grün) |

**Bilanzzeile:** „Was erzeugt wird, wird verbraucht, gespeichert oder fließt über die Leitung.“
**Aha:** „Der Akku verschiebt die Mittagssonne in den Abend. Die Leitung muss gar nicht dicker werden.“
Das ist Kalles Problem aus doc 05.

### MG07 – Hitzewelle (Finale): „Ein Kopplungsschritt“

**Tafel:** keine neuen Formeln, sondern eine Stunde, die nacheinander durch drei Modelle läuft: *Hitze →
Gebäude brauchen Kühlung (MG01) → Stromnetz (MG06) → Bahn und Hafen (MG05/MG04)*. Je Modell eine Zeile mit „bekommt“
und „gibt weiter“.

**Diskretisierung:** Co-Simulation mit festem Kopplungsschritt $\Delta T$ (1 Stunde), Reihenfolge fest
(Gauß-Seidel-Kopplung). Innerhalb eines Kopplungsschritts rechnet jedes Modell mit seinem eigenen Schritt:

$$Q_{\mathrm{kühl}} = UA\,(T_{\mathrm{außen}} - T_{\mathrm{soll}}) + Q_{\mathrm{Sonne}},\qquad P_{\mathrm{el}} = \frac{Q_{\mathrm{kühl}}}{\mathrm{COP}}\ \to\ P_i \text{ in } B\,\theta = P$$

**Aha:** „Jedes Modell kennt nur seinen Teil. Erst zusammen wird daraus ein Digitaler Zwilling.“
Erst planen, wenn MG01, MG04, MG05 und MG06 stehen.

## 4. Technik: eine Rechentafel für alle

**Komponente.** `PairLessonView` (MG1) wird zur allgemeinen Rechentafel `StepTableView`. Sie bekommt:
- Spalten aus dem Level-JSON (Textschlüssel, Farbe aus `Palette`, Rechenzeichen, Formelschlüssel für das Pop-up),
- Zeilen = Elemente des Beispiels,
- ein Modell-Interface im Sim-Kern ohne UnityEngine:
  `IStepLesson { void Reset(); StepRow[] Step(); string Balance(); bool Done; }`.

Die 3D-Ansicht bleibt je Minigame eigen: Felder, Päckchen, Züge, Balken.

**Ablauf je Minigame** (Architekturgesetz „Python zuerst“):
1. `sim-reference/simref/<minigame>_step.py`: kleines Beispiel und Zerlegung `step_terms`. Beim großen Modell
   bekommt jedes Beispiel eine Funktion, die zeigt, dass der Endzustand des Beispiels = großes Modell für dieselben Elemente.
2. Golden File `golden/<bereich>/pair_<minigame>.json`. Name nicht mit dem Präfix eines Raster-Globs beginnen,
   siehe Stolperstelle `settlement_*` in MG1.
3. C# in `sim-core` (netstandard2.1, kein UnityEngine), NUnit gegen das Golden File.
4. Formeln in `ui_de.json` → `python tools/render_formulas.py`, Spaltentexte in `ui_de.json`, Text-Lint grün.
5. View, Handprobe als Issue im Projekt „Pinguin-Spiel“ (Skill `/pinguin-simulation-pull-request`).

**Pflicht-Tests je Rechenschritt:**
- **Zerlegung geht exakt auf:** *neu − vorher = Summe der Spalten* (MG1: Wärme; MG2: Luft; MG3: Kisten; MG05: Weg;
  MG06: Strom; MG03: Kräfte).
- **Austausch summiert zu null:** Was der eine bekommt, gibt der andere ab.
- **Endzustand = großes Modell:** Das Beispiel ist ein echter Ausschnitt, keine Lehr-Sonderrechnung.
- **Mutationsprobe:** Regel in einer Kopie brechen, ein Test muss rot werden (Definition of Done).

## 5. Reihenfolge und Aufwand (Vorschlag)

| Reihenfolge | Was | Warum zuerst | Aufwand grob |
|---|---|---|---|
| 1 | `StepTableView` aus `PairLessonView` herauslösen, MG1 darauf umstellen | Grundlage für alle anderen, MG1 bleibt Prüfstein | 1–2 Tage |
| 2 | MG01 Hitzeinsel | gleiches Modell wie MG1, testet nur die Verallgemeinerung | 1 Tag |
| 3 | MG3/MG04 Fischfabrik | Vertical-Slice-Kandidat der Erweiterung, ganz andere Art (Ereignisse) | 2 Tage |
| 4 | MG2 Schlitten | Release 1, neue Art (Päckchen), braucht eigene 3D-Ansicht | 3 Tage |
| 5 | MG06 Netz-Balance | Release 1, nutzt Löser und Tafel von MG1 | 1–2 Tage |
| 6 | MG05, MG02, MG03 | Release 2 | je 2 Tage |
| 7 | MG2-B, MG07 | erst nach den Entscheidungen zu Bonus und Finale | offen |

## 6. Offene Entscheidungen für Jona

1. **Platz im Ablauf:** immer Ende Phase 2 wie MG1, oder wie oben vorgeschlagen bei Brücke und Fischfabrik in Phase 4?
2. **MG2 Schlitten:** 1D-Tafel mit Zahlen plus 2D-Animation ohne Zahlen (Vorschlag), oder gleich ein 3 × 3-Ausschnitt in 2D?
3. **Ehrliche Vereinfachungen:** MG02 (Ausgleichsrunden statt CG) und MG03 (Nachgeben statt direktem Lösen) zeigen
   auf der Tafel ein langsameres Verfahren mit demselben Ergebnis. Reicht der Hinweis unter der Haube?
4. **Einheiten:** Auf der Tafel ohne Einheiten (Text-Lint) und unter der Haube mit, oder Einheiten auf der Tafel erlauben
   und den Lint dafür lockern?
5. **Notizbuch-Begriffe** je Minigame: „Zeitschritt“ (MG1), „Gleichgewicht“, „Ereignis“, „Kopplung“ … festlegen.
