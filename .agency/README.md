# .agency – Arbeitsweise für KI-gestützte Entwicklung

Übernommen aus dem *Agency Blueprint* (Hackathon-Playbook „Agentic Development Cycle“, Rev. 1.1)
und auf dieses Spiel zugeschnitten. Vom Blueprint bleiben **Rollen, harte Gates, Review-Inbox,
Entscheidungslog und Retrospektive**. Weggelassen ist der Web-Stack (Next.js, FastAPI, Postgres,
SonarQube): Hier ist die „Iron Pipeline“ das, was dieses Repository wirklich prüfen kann.

| Ordner | Inhalt |
|---|---|
| `roles/` | Wer was entscheidet; jede Rolle mit Veto-Recht oder ohne |
| `rules/definition_of_done.md` | Wann eine Aufgabe fertig ist – die Gates, die alle grün sein müssen |
| `rules/architectural_laws.md` | Unverhandelbare Regeln der Codebasis (Langfassung der CLAUDE.md-Regeln) |
| `memory/decisions.md` | Entscheidungen mit Begründung und verworfener Alternative (ADR) |
| `memory/context.md` | Aktueller Stand: was gebaut ist, was offen ist |
| `agent_inbox/` | Asynchrone Übergaben zwischen Rollen (Review-Anfragen, Befunde) |
| `scrum_master/learnings_database.md` | Meta-Learnings aus Retrospektiven mit Gegenmaßnahme |

Ablauf einer Aufgabe: **PRODUCT_OWNER** formuliert Ziel + Abnahmekriterium → **SIM_ENGINEER** /
**UNITY_DEV** setzen um → **TEST_DEV** belegt es → **QA_ENGINEER** prüft gegen die Definition of Done
(Inbox) → **SCRUM_MASTER** hält fest, was gehakt hat.
