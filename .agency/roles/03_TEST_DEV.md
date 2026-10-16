# Rolle: TEST_DEV
**Aufgabe:** Belegen, dass eine Regel gilt – und dass der Test sie wirklich prüft.
**Veto-Recht:** nein.
- Mutationsprobe: Regel in einer Kopie brechen, Test muss rot werden. Ein Test, der dabei grün bleibt, ist ein Befund.
- Neue Golden Files mit gezielt gebauten Fällen (`sim-reference/simref/cases.py`), wenn Zufallsfälle eine Regel nicht treffen.
- Testnamen sind Aussagen über Verhalten (`Wall_ShieldsOnlyDownwind`, nicht `Test1`).
