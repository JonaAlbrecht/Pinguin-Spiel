# 06 – Open Questions for the First Prototype

> Purpose: This list collects the decisions that are missing for a complete user specification and a complete technical specification of the first prototype.
> Basis: docs 00–05 at commit `b633926` (GitLab `main`, 16.10.2026).
> Assumption: The first prototype is the "Vertical Slice" from doc 01, section 6 (hub section "Huddle-Platz", prologue, MG1 Huddle, one transfer screen). Question A-01 confirms or changes this assumption.
> How to use: Write the answer in the last column. If a question is not relevant, write "n/a" and the reason. Questions in section 0 block the most other work.

---

## 0. Blocking questions (answer these first)

| ID | Question | Why it blocks | Answer |
|---|---|---|---|
| 0-01 | Is the first prototype the Vertical Slice (prologue + MG1 + one transfer screen), or something smaller or larger? | Scope of all other answers depends on it. | |
| 0-02 | Is a Unity Pro/Enterprise/Industry licence available for the team and for CI? If not, which engine do we use? | Doc 04 states that Unity Personal is not permitted at Siemens. Without a licence, the full stack changes. | |
| 0-03 | Which repository is the source of truth: GitLab (`code.siemens.com`, internal) or GitHub (`JonaAlbrecht/Pinguin-Spiel`, **public**)? | Doc 04 says "treat as Restricted". The GitHub repo is public and 4 commits behind. CI is specified as GitHub Actions. | |
| 0-04 | Who is the product owner, and who gives the final go for the prototype? | Many questions below need one decision maker. | |
| 0-05 | What is the deadline, and is the prototype tied to an event (learning event, onboarding, fair)? | It sets the scope cut and the target platform (browser or Windows standalone). | |
| 0-06 | Who is in the team (people, roles, hours per week)? Who does 3D art, game code, simulation code, writing? | The spec cannot assign work or estimate effort without it. | |
| 0-07 | Who approves the Siemens content of the MG1 transfer screen (switchgear cabinet, SIMARIS therm, Simcenter 3D)? | Doc 04, section 5.7: the build fails without an approved card. | |
| 0-08 | Where does the prototype run for the playtest: Siemens intranet web server, Teams/SharePoint link, local Windows build? | It decides the build target, hosting tasks, and IT contacts. | |

---

## Part 1 – User specification

### A. Goal and scope of the prototype

| ID | Question | Context | Answer |
|---|---|---|---|
| A-01 | Confirm the scope: which of these items are in the prototype? Hub walk, prologue (MG1 part A), MG1 part B, transfer screen, notebook, cosmetic rewards, save, "Blick unter die Haube", quiz, EN language. | Doc 01 lists only hub section, MG1, 1 transfer screen, prologue dialogues. The other items are not clearly in or out. | |
| A-02 | What question must the prototype answer? Examples: "Do non-technical users understand the idea of a simulation?", "Is the art style accepted?", "Is the tech stack feasible?" | It defines success and the playtest design. | |
| A-03 | What are the go/no-go criteria for Release 1 after the prototype? | Doc 04, M3 has a playtest, but no decision rule. | |
| A-04 | Which stakeholders must see the prototype (sponsor, business units DI/SI/Mobility, HR Learning, Communications)? | It affects polish level and branding needs. | |
| A-05 | What is the target play time of the prototype? Doc 02 gives prologue ~3 min + act 1 ~10 min. Is ~15 min correct? | Sets content volume. | |
| A-06 | What is the official name of the game? Options in the docs: "Vom Huddle zur Siedlung", "Simulation for Everyone", "Pinguin-Spiel", repo name "stand-simulation-for-everyone". | Needed for title screen, URL, and build name. | |

### B. Target group and play context

| ID | Question | Context | Answer |
|---|---|---|---|
| B-01 | Which devices do players use: Siemens office laptop only, or also tablets and phones? | Doc 00 says mouse/touch. Doc 04 says WebGL + Windows standalone. Mobile WebGL has memory limits. | |
| B-02 | Which browsers must work (Edge, Chrome, Firefox, Safari)? | Defines the test matrix. | |
| B-03 | Do players have sound available (open-plan office)? Is sound required to understand the game? | Defines audio and subtitle needs. | |
| B-04 | Do players play alone, or in a moderated group session (for example, a learning event with a facilitator)? | Affects tutorial depth and help system. | |
| B-05 | Is a facilitator guide or a short introduction slide part of the prototype? | Optional deliverable. | |
| B-06 | Does the secondary target group (technical colleagues, "Blick unter die Haube") matter for the prototype at all? | Doc 00 calls it optional. Scope question. | |

### C. Hub and navigation

| ID | Question | Context | Answer |
|---|---|---|---|
| C-01 | How does the player move Pip in the hub: click-to-move, drag, keyboard (WASD), virtual joystick? | Doc 00 says "mouse/touch only". No movement scheme is defined. | |
| C-02 | How large is the hub section "Huddle-Platz" in the prototype, and what does it contain (NPCs, objects, signs)? | Only the name is defined. | |
| C-03 | How does the player start the minigame: talk to Frieda, walk into a zone, click a sign? | Interaction model is not defined. | |
| C-04 | Does the hub change after MG1 (igloos and lanterns appear)? If yes, does it show the player's own layout or a fixed layout? | Doc 01, section 5 promises visible growth. | |
| C-05 | Is there a main menu (start, continue, settings, language, credits)? What does it contain? | Not specified. | |
| C-06 | Can the player leave a minigame at any time and return to the hub? What happens to the progress? | Doc 00: "interruptible at any time". | |

### D. Prologue – MG1 part A "Der letzte Huddle"

| ID | Question | Context | Answer |
|---|---|---|---|
| D-01 | How many penguins are in the huddle? | Not specified. | |
| D-02 | How does the player move penguins: one by one with drag, or the group as a whole? | "Pip schiebt Pinguine" is not precise enough to build. | |
| D-03 | What does "the timer runs only when the player does nothing" mean exactly? Does it count 60 s of stable state? | The sentence in doc 03 is ambiguous. | |
| D-04 | How often does the wind turn, and does the player see it before it turns? | Defines difficulty. | |
| D-05 | Can the player fail the prologue? What happens if a penguin freezes? | Doc 00: no game over. A recovery rule is missing. | |
| D-06 | Does the prologue run in the hub scene or in its own scene? | Affects scene structure. | |
| D-07 | Is the prologue skippable on a second play? | Replay comfort. | |

### E. MG1 part B "Die erste Siedlung" – phases

| ID | Question | Context | Answer |
|---|---|---|---|
| E-01 | Phase 1 "Was ist wichtig?": how many cards, how many right and wrong ones, what happens on a wrong pick? | Doc 03 lists 6 cards as an example. | |
| E-02 | Phase 2 "Welche Regel gilt?": how does the scale interaction work exactly (drag symbols onto two pans)? What is the success condition? | Only described as an image. | |
| E-03 | Phase 3 "Alles hängt zusammen": how many heat threads does the player draw by hand before the computer takes over? | Focus phase. Not quantified. | |
| E-04 | Is the settlement grid 24 × 24 cells of 2 m? How many cells does one igloo, one lantern, one snow wall occupy? | Grid size is defined, building footprints are not. | |
| E-05 | Does the level start empty or with pre-placed igloos, lanterns, terrain? | `initialTiles` is a placeholder. | |
| E-06 | What costs budget: only lanterns, or also igloos and snow walls? Is the number of snow walls limited? | Doc 04 shows only `lantern.cost`. | |
| E-07 | How does the player place, move, and delete objects (drag & drop, brush, palette)? Is there undo? | Input tools are not defined. | |
| E-08 | Does the player press "Simulieren" by hand in each round, or does the heat map update live while building? | Changes the didactic effect and the performance budget. | |
| E-09 | What happens after round 3 if the target is not reached? | Doc 03: 3 rounds. Doc 03 also: hint after 2 failures, solution help after 3. These rules collide. | |
| E-10 | What is the "solution help" concretely: show a correct layout, highlight cells, place objects automatically? | Not defined. | |
| E-11 | Can melted igloos be rebuilt in the next round? Does a melted igloo cost anything? | Melt cascade rule is defined only in technical terms. | |
| E-12 | Is the wind direction visible to the player (arrow, snow particles)? | Needed to make snow walls understandable. | |
| E-13 | Is there more than one level or difficulty for MG1? | Doc 04 shows one level file. | |

### F. Scoring, help, replay

| ID | Question | Context | Answer |
|---|---|---|---|
| F-01 | Confirm the star rules: ★ target igloo count, ★★ no igloo melted, ★★★ under 80 % budget. Is the target 12 igloos? | Values from doc 03 and doc 04. Not balanced yet. | |
| F-02 | Do stars count across rounds (best round) or only the last round? | Not defined. | |
| F-03 | Is replay for a better score ("Optimierungs-Loop") part of the prototype? | Doc 01 lists it in the core loop. | |
| F-04 | What exactly do the traffic lights show, and which thresholds switch green/yellow/red? | `ResultTranslator` needs fixed rules. | |
| F-05 | Does Frieda react to the player result with different dialogue lines? | Dialogue volume. | |

### G. Transfer screen "So macht's Siemens"

| ID | Question | Context | Answer |
|---|---|---|---|
| G-01 | Which real Siemens example is shown for MG1: SIMARIS therm (SI) or Simcenter 3D (DISW) or both? | Doc 00 lists both as proposals. | |
| G-02 | Which image or video is available and approved? Who provides it? | Required field in the card. | |
| G-03 | Do business experts accept the simplification "comfort lower limit is a game element"? | Doc 03 states it openly. Experts must agree. | |
| G-04 | Is the "Siemens-Stimme" video clip part of the prototype? If yes, who records it? | Optional per doc 00. | |
| G-05 | Is a draft card marked "Entwurf" allowed in the playtest build? | Doc 04 allows drafts only in development builds. | |
| G-06 | Is there an intranet link for "Mehr erfahren"? | Optional field. | |
| G-07 | Do we use the Siemens logo and corporate design (fonts, colours) in the game? Who approves the branding? | Doc 04 allows only OFL fonts. Siemens Sans is not OFL. | |

### H. Notebook, rewards, progression

| ID | Question | Context | Answer |
|---|---|---|---|
| H-01 | Is the notebook (Siemens map, glossary, take-away sentences) in the prototype? | Doc 01 describes it. Vertical Slice scope does not mention it. | |
| H-02 | Which glossary terms are in MG1? One new term per phase is the rule. | Content list is missing. | |
| H-03 | Are cosmetic rewards and Pip customisation (hat, scarf, colour) in the prototype? | Extra art effort. | |
| H-04 | Is the ZWILLI tablet a real UI element (menu, notebook) or only a story object? | UI design depends on it. | |

### I. Story, dialogue, text

| ID | Question | Context | Answer |
|---|---|---|---|
| I-01 | Who writes the final dialogue for prologue and act 1? Who reviews it (Communications, language check)? | Doc 02 has only example scenes. | |
| I-02 | Is Pip gender-neutral, or can the player choose? | Doc 02 uses "Chefingenieur:in". | |
| I-03 | Do NPCs speak with voice-over, gibberish sounds, or text only? | Audio and localisation effort. | |
| I-04 | Is the text-lint (forbidden-word list) part of the prototype build? Who maintains the list? | Doc 04, section 5.6. | |
| I-05 | Which characters appear in the prototype: Pip, Frieda, ZWILLI, Kalle, Oma Grete? Does "Kalles Versuch" play as a cut-scene or as an interactive demo? | Animation effort. | |

### J. Art, audio, UI style

| ID | Question | Context | Answer |
|---|---|---|---|
| J-01 | Is there a reference mood board or concept art? Who approves the style test (M0)? | Doc 01 names reference games only. | |
| J-02 | Do we build own 3D assets in Blender, use CC0 assets, or mix both? | Doc 04 allows both. | |
| J-03 | Which animations are needed for the prototype? Doc 04 lists 10 clips. | Effort estimate. | |
| J-04 | Music and sound effects: needed? Source (CC0, commissioned, none)? | Audio is not specified in any doc. | |
| J-05 | Is there a UI style guide (fonts, icon set, colours, button style)? | Needed for UI Toolkit work. | |

### K. Accessibility and language

| ID | Question | Context | Answer |
|---|---|---|---|
| K-01 | Is the prototype German only, or German and English? | Doc 04 puts EN in M4 (Release 1). | |
| K-02 | Which accessibility level is required: WCAG AA contrast only, or also keyboard-only and screen reader? Are there Siemens accessibility rules for internal learning tools? | Doc 04 names WCAG-AA contrast and colour-blind palettes. | |
| K-03 | Is the read-aloud function (text-to-speech) in the prototype? | Optional per doc 04. | |
| K-04 | Do we need adjustable text size and a reduced-motion option? | Doc 04 mentions scalable text only. | |

### L. Learning measurement and playtest

| ID | Question | Context | Answer |
|---|---|---|---|
| L-01 | What are the 5 pre/post quiz questions, and what are the correct answers? | Doc 00 gives one example only. | |
| L-02 | Is the quiz inside the game or outside (for example, Microsoft Forms)? | Telemetry is off and there is no backend. In-game answers have no storage. | |
| L-03 | Who recruits the 10–15 playtest participants, and from which departments? | Doc 04, M3. | |
| L-04 | Do we record playtests (screen, audio, observation notes)? Do we need consent forms, works council (Betriebsrat), or data protection approval? | Personal data in user tests. | |
| L-05 | Which observations do we record per participant (time per phase, help used, quotes)? | Defines a test protocol. | |

---

## Part 2 – Technical specification

### M. Engine, licences, tools

| ID | Question | Context | Answer |
|---|---|---|---|
| M-01 | If no Unity licence is available in time, is a fallback allowed (Godot, Babylon.js/three.js)? | The sim-core in pure C# stays reusable in Godot. A web engine needs a port. | |
| M-02 | Which exact Unity 6 LTS version do we pin? | Doc 04 requires a pinned version. | |
| M-03 | Is ink allowed (MIT licence) by Siemens open-source rules? Who checks third-party licences (OSS clearing)? | Doc 04 uses ink, Newtonsoft, CC0 assets. | |
| M-04 | Is Blender allowed on Siemens machines, and who has it installed? | Pipeline in doc 04, section 6. | |
| M-05 | Can we use AI-generated code and AI-generated art in this project? Are there Siemens rules for AI-generated assets? | Doc 04 relies on AI-supported development. | |

### N. Repository, CI, classification

| ID | Question | Context | Answer |
|---|---|---|---|
| N-01 | Do we keep, archive, or make private the public GitHub repo? | Content includes internal concept and Siemens product names. Doc 04 says "Restricted". | |
| N-02 | Do we move CI from GitHub Actions to GitLab CI on `code.siemens.com`? | GitLab CI is enabled on the project. Doc 04 assumes GitHub Actions and GameCI. | |
| N-03 | Are GitLab runners with Windows/Linux and enough disk for Unity builds available? How is the Unity licence activated in CI? | Unity batchmode builds need a licence secret. | |
| N-04 | Is Git LFS storage quota on `code.siemens.com` sufficient for `.blend`, `.fbx`, textures, audio? | LFS is enabled. Quota is unknown. | |
| N-05 | Do we keep the Unity project folder name `unity/PinguinCity`? | The name comes from the old Smart City concept. | |
| N-06 | Which branch and MR rules apply (protected `main`, reviews, MR template)? | Not defined. | |

### O. Sim-core MG1 – model parameters

| ID | Question | Context | Answer |
|---|---|---|---|
| O-01 | Which values do the material parameters have: `k` between cells (ice, igloo, water, path), `h` on open area and behind snow walls? | `materials_settlement_v1` is only a name. Balancing needs numbers. | |
| O-02 | Does an igloo produce heat itself (penguins inside), or only lanterns? | Doc 03: `q_i > 0` only in lantern cells. | |
| O-03 | How does wind direction change `h_i`? How many cells behind a snow wall are protected? | The level JSON has `wind` and `windDirection`. The model has only `h_i`. | |
| O-04 | Is the temperature of an igloo the value of one cell, or the mean of its footprint cells? | Depends on E-04. | |
| O-05 | Confirm thresholds: air −30 °C, comfortable −8 °C ≤ T < 0 °C, melting T ≥ 0 °C. | Doc 03 values. | |
| O-06 | Is the balancing target reachable: 12 igloos with ≤ 10 lanterns (budget 1000, cost 100), and ★★★ with ≤ 8 lanterns? | Must be shown in the Python reference before the build. | |
| O-07 | Huddle model (part A): how are neighbours defined (distance threshold, grid contact)? Which values do `C`, `G_ij`, `h_i`, time step have? | Doc 03 gives only the equation. | |
| O-08 | Are positions in the huddle continuous or on a grid? | It changes the neighbour logic and the input. | |
| O-09 | Is the Python reference plus golden files mandatory for the prototype, or only for Release 1? | Effort versus confidence. | |
| O-10 | Does the "Simulieren" animation show real CG iterations step by step, or a faked fade-in of the final result? | Doc 04 makes all solvers steppable for this purpose. | |

### P. Unity architecture

| ID | Question | Context | Answer |
|---|---|---|---|
| P-01 | Do we build the full framework (EventBus, services, `IMinigame`, phase state machine) in the prototype, or a lean version? | Effort versus reuse in Release 1. | |
| P-02 | Which scenes exist in the prototype: Bootstrap, Hub, MG1_Huddle, Transfer? Is the prologue a separate scene? | See D-06. | |
| P-03 | Do we generate scenes by the `SceneBuilder` editor script, or build them by hand in the prototype? | Doc 04 prefers generation. | |
| P-04 | Which camera setup do we use (Cinemachine blend hub → top-down)? Is the "Curved World" shader in scope? | Doc 04, section 5.8. | |
| P-05 | Which heat-map colour scale do we use: viridis/cividis (doc 04) or blue-green-red (doc 03)? | The two docs conflict. Blue-green-red is not colour-blind safe. | |
| P-06 | Which UI is built with UI Toolkit and which with uGUI (world-space speech bubbles)? | Doc 04 leaves it open. | |

### Q. Platform, performance, hosting

| ID | Question | Context | Answer |
|---|---|---|---|
| Q-01 | Which reference laptop model is the performance target ("office laptop with iGPU")? | Budgets: 60/30 FPS, < 300 draw calls, ≤ 50 MB, hub load ≤ 15 s. | |
| Q-02 | Which intranet hosting platform serves the WebGL build with correct Brotli/MIME headers? Who is the IT contact? | Doc 04, section 9. | |
| Q-03 | Must the build be packaged for a learning platform (SCORM or xAPI) for completion tracking? | Doc 04 mentions LMS integration. It conflicts with "telemetry off". | |
| Q-04 | Is access restricted (Siemens SSO, Entra ID), or is a hidden URL enough? | Classification "Restricted". | |
| Q-05 | Do we need a Windows standalone build for the prototype? | Doc 04 lists it for fairs and events. | |

### R. Data, save, privacy

| ID | Question | Context | Answer |
|---|---|---|---|
| R-01 | Is save/continue in the prototype? | Doc 00: "interruptible at any time". Doc 04, M4: save in Release 1. | |
| R-02 | Is any telemetry allowed in the playtest build (for example, anonymous time per phase)? Who approves it (data protection, works council)? | Doc 04: telemetry off by default. | |
| R-03 | Is the leaderboard out of scope for the prototype? | Doc 04 marks it optional and subject to works council. | |
| R-04 | Is a privacy notice or imprint page needed in the game? | Depends on hosting and data collection. | |

### S. Later decisions that affect the prototype architecture

| ID | Question | Context | Answer |
|---|---|---|---|
| S-01 | MG2: Polar-Express (option A) or switchgear cooling (option B)? | Doc 01, decision 1. B needs temperature transport in the LBM core. | |
| S-02 | Is there a fourth core minigame ("missing number 3" on the Loop page)? Candidates: grid balance, bridge. | Doc 01, decision 2. It affects the hub layout. | |
| S-03 | Is the Pingopolis extension still planned? If not, do we remove its modules (`Structure`, `Rail`, `Grid`, `Coupling`) from the repo structure? | Doc 04 and doc 05 carry a large extension scope. | |
| S-04 | Does MG3 use the MG2 result (train interval) as input? | Doc 03 calls it optional. It affects the save data model. | |

---

## Inconsistencies found in docs 00–05

| # | Finding | Where |
|---|---|---|
| 1 | The GitHub remote is public. Doc 04 says to treat documents and builds as "Restricted". | Repo remotes, doc 04 section 9 |
| 2 | CI is specified as GitHub Actions. The active remote is GitLab with CI enabled. | Doc 04 sections 2 and 3 |
| 3 | The heat map colours differ: blue/green/red in doc 03, viridis/cividis in doc 04. | Doc 03 MG1 phase 4, doc 04 section 5.5 |
| 4 | 3 rounds versus "hint after 2 failures, solution help after 3" leaves no round for the solution help. | Doc 03 general rules and MG1 phase 5 |
| 5 | City leftovers remain in the core docs: `unity/PinguinCity`, "Stadt-Baukasten", building footprints in multiples of 4 m, hub scene "Stadt". | Doc 04 sections 3, 5.1, 6.1 |
| 6 | Wind direction is in the level data, but the steady-state model has no rule for direction. | Doc 03 MG1 technology, doc 04 section 5.4 |
| 7 | The Loop export in `export/` still describes Smart City as the core concept, and it is not committed. | `export/01_Konzept_SmartCity_loop.md` |
