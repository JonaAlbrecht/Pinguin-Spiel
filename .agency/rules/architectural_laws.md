# Architektur-Gesetze

1. **Der Simulationskern kennt Unity nicht.** `sim-core/.../Runtime`: netstandard2.1, C# 9, kein `record`/`init`,
   kein `System.Text.Json`, keine Threads, JSON nur über `SFE.SimCore.Data.MiniJson`. Grund: derselbe Code läuft
   in `dotnet test` (CI ohne Unity) und im Spiel, auch in WebGL.
2. **Python zuerst.** Jede Rechenregel entsteht in `sim-reference/simref/*.py`, wird dort getestet und erzeugt
   Golden Files. C# folgt, bis es auf 1e-6 relativ übereinstimmt. Die Referenz ist die Wahrheit, nicht der Spielcode.
3. **Daten liegen nur einmal.** Level, Materialien, Texte: `unity/PinguinKolonie/Assets/_Project/Resources/Data/`.
   Python, dotnet-Tests und Unity lesen dieselben Dateien.
4. **Texte gehören in JSON.** Kein Spieltext als C#-Literal; der Text-Lint prüft beides.
5. **Alles per Code.** Keine Szenen- oder Prefab-YAML von Hand. Die Szene enthält nur `GameRoot`.
6. **Nur Zeichen der eingebauten Schrift.** Keine Emoji, kein ★ ✔ ▸ ▲ ▼ – auch nicht in C#-Strings.
7. **Ehrlich vereinfachen.** Was das Spiel als Modell zeigt, muss das Modell auch rechnen. Keine Fake-Animation,
   die eine Rechnung vortäuscht (O-10); wo vereinfacht wird, sagt das Spiel es.
