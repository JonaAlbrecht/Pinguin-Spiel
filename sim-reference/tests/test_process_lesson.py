"""MG3 Schritt 2: Die Rechentafel einer Station rechnet mit derselben Simulation wie die ganze Fabrik."""
from pathlib import Path

from simref import process as p
from simref import process_lesson as L

DATA = Path(__file__).resolve().parents[2] / "unity/PinguinKolonie/Assets/_Project/Resources/Data"
LESSON = L.load(DATA / "Levels/MG3_Fischfabrik.json")


def test_every_row_adds_up():
    """Kisten vorher + kommen an - werden fertig = Kisten neu – Kisten verschwinden nicht."""
    rows = L.run(LESSON)
    assert len(rows) == LESSON.events
    for r in rows:
        assert r.before + r.arrived - r.finished == r.after
        assert r.arrived + r.finished == 1          # genau ein Ereignis je Zeile
        assert r.waiting + r.working == r.after
    for a, b in zip(rows, rows[1:]):
        assert b.before == a.after and b.time >= a.time


def test_the_table_tells_a_story():
    """Lesbar und lehrreich: Der Stapel wächst auf mindestens 3, leert sich zwischendurch ganz und wächst wieder."""
    after = [r.after for r in L.run(LESSON)]
    assert max(after) >= 3
    first_empty = after.index(0)
    assert 2 <= first_empty < len(after) - 2
    assert max(after[first_empty:]) >= 2
    assert L.run(LESSON)[0].kind == "arrive"


def test_lesson_is_the_same_simulation_as_the_factory():
    """Keine zweite Wahrheit: Die Zeiten der Tafel sind die Ereignisse von FactorySimulation."""
    sim = p.FactorySimulation(L.config(LESSON), LESSON.seed, trace=True)
    for _ in range(LESSON.events):
        sim.step()
    assert [r.time for r in L.run(LESSON)] == [t for t, *_ in sim.trace]
