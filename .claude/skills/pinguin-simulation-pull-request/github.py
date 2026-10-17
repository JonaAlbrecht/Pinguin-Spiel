"""GitHub-Helfer für den Skill pinguin-simulation-pull-request (kein `gh` auf diesem Rechner).

    python .claude/skills/pinguin-simulation-pull-request/github.py status [--branch NAME]
    python .claude/skills/pinguin-simulation-pull-request/github.py handprobe --title "..." --body-file datei.md
    python .claude/skills/pinguin-simulation-pull-request/github.py pr --title "..." --body-file datei.md [--draft]

status     zeigt, ob es zum Branch schon einen PR gibt und ob er gemergt ist (Jona merged zwischendurch selbst).
handprobe  legt ein Issue „Handprobe: …“ mit Label `handprobe` an und hängt es ins GitHub-Projekt „Pinguin-Spiel“
           (Status Todo). Gibt die Issue-Nummer aus – die gehört in die PR-Beschreibung.
pr         öffnet den Pull Request vom aktuellen (gepushten) Branch nach main.

Token: Datei aus der Umgebungsvariable GITHUB_TOKEN_FILE, sonst die lokale Standarddatei unten (Scopes `repo` +
`project`); ohne Datei der Token aus `git credential fill` (reicht für PR/Issue, nicht fürs Projekt).
Der Token wird nie ausgegeben – auch nicht in Fehlermeldungen.
"""
from __future__ import annotations

import argparse
import json
import os
import subprocess
import sys
import urllib.error
import urllib.request
from pathlib import Path

OWNER, REPO, PROJECT_TITLE = "JonaAlbrecht", "Pinguin-Spiel", "Pinguin-Spiel"
DEFAULT_TOKEN_FILE = Path.home() / "Algo-Trading" / "01_remembrance_project" / "new-github-token.txt"


def token() -> tuple[str, bool]:
    """(Token, hat project-Scope?)"""
    f = Path(os.environ.get("GITHUB_TOKEN_FILE", DEFAULT_TOKEN_FILE))
    if f.exists():
        return f.read_text(encoding="utf-8").strip(), True
    out = subprocess.run(["git", "credential", "fill"], input="protocol=https\nhost=github.com\n\n",
                         capture_output=True, text=True).stdout
    for line in out.splitlines():
        if line.startswith("password="):
            return line.split("=", 1)[1], False
    sys.exit("Kein GitHub-Token gefunden (GITHUB_TOKEN_FILE oder git credential).")


TOKEN, HAS_PROJECT = token()


def rest(method: str, path: str, data: dict | None = None):
    req = urllib.request.Request(f"https://api.github.com{path}", method=method,
                                 data=json.dumps(data).encode() if data is not None else None,
                                 headers={"Authorization": f"token {TOKEN}", "Accept": "application/vnd.github+json"})
    try:
        return json.load(urllib.request.urlopen(req))
    except urllib.error.HTTPError as e:
        sys.exit(f"GitHub {method} {path}: {e.code} {e.read().decode(errors='replace')[:400]}")


def gql(query: str, **variables):
    req = urllib.request.Request("https://api.github.com/graphql", method="POST",
                                 data=json.dumps({"query": query, "variables": variables}).encode(),
                                 headers={"Authorization": f"bearer {TOKEN}"})
    res = json.load(urllib.request.urlopen(req))
    if "errors" in res:
        sys.exit("GraphQL: " + json.dumps(res["errors"], ensure_ascii=False)[:600])
    return res["data"]


def branch() -> str:
    return subprocess.run(["git", "branch", "--show-current"], capture_output=True, text=True).stdout.strip()


def cmd_status(args):
    b = args.branch or branch()
    prs = rest("GET", f"/repos/{OWNER}/{REPO}/pulls?state=all&head={OWNER}:{b}")
    if not prs:
        print(f"{b}: noch kein Pull Request")
    for p in prs:
        state = "gemergt" if p.get("merged_at") else p["state"]
        print(f"{b}: PR #{p['number']} {state} – {p['html_url']}")


def add_to_project(content_id: str) -> str:
    if not HAS_PROJECT:
        sys.exit("Token ohne project-Scope – Issue ist angelegt, aber nicht im Projekt. GITHUB_TOKEN_FILE setzen.")
    d = gql("{ viewer { projectsV2(first: 50) { nodes { id title url } } } }")
    project = next((p for p in d["viewer"]["projectsV2"]["nodes"] if p["title"] == PROJECT_TITLE), None)
    if project is None:
        sys.exit(f"GitHub-Projekt „{PROJECT_TITLE}“ nicht gefunden.")
    item = gql("""mutation($p: ID!, $c: ID!) { addProjectV2ItemById(input: {projectId: $p, contentId: $c}) { item { id } } }""",
               p=project["id"], c=content_id)["addProjectV2ItemById"]["item"]["id"]
    field = gql("""query($p: ID!) { node(id: $p) { ... on ProjectV2 { field(name: "Status") {
                 ... on ProjectV2SingleSelectField { id options { id name } } } } } }""", p=project["id"])["node"]["field"]
    if field:
        todo = next((o for o in field["options"] if o["name"].lower() in ("todo", "to do")), field["options"][0])
        gql("""mutation($p: ID!, $i: ID!, $f: ID!, $o: String!) { updateProjectV2ItemFieldValue(input: {projectId: $p,
               itemId: $i, fieldId: $f, value: {singleSelectOptionId: $o}}) { projectV2Item { id } } }""",
            p=project["id"], i=item, f=field["id"], o=todo["id"])
    return project["url"]


def cmd_handprobe(args):
    title = args.title if args.title.startswith("Handprobe") else f"Handprobe: {args.title}"
    labels = {l["name"] for l in rest("GET", f"/repos/{OWNER}/{REPO}/labels?per_page=100")}
    if "handprobe" not in labels:
        rest("POST", f"/repos/{OWNER}/{REPO}/labels",
             {"name": "handprobe", "color": "0e8a16", "description": "Manuelle Prüfung im Unity-Editor"})
    issue = rest("POST", f"/repos/{OWNER}/{REPO}/issues",
                 {"title": title, "body": Path(args.body_file).read_text(encoding="utf-8"), "labels": ["handprobe"]})
    url = add_to_project(issue["node_id"])
    print(f"Handprobe #{issue['number']}: {issue['html_url']}  (Projekt: {url}, Status Todo)")


def cmd_pr(args):
    b = branch()
    if b in ("main", "master", ""):
        sys.exit("Auf main – erst einen Branch anlegen.")
    open_prs = rest("GET", f"/repos/{OWNER}/{REPO}/pulls?state=open&head={OWNER}:{b}")
    if open_prs:
        sys.exit(f"Für {b} ist schon PR #{open_prs[0]['number']} offen: {open_prs[0]['html_url']}")
    pr = rest("POST", f"/repos/{OWNER}/{REPO}/pulls",
              {"title": args.title, "head": b, "base": args.base, "draft": args.draft,
               "body": Path(args.body_file).read_text(encoding="utf-8")})
    print(f"PR #{pr['number']}: {pr['html_url']}")


def main():
    ap = argparse.ArgumentParser()
    sub = ap.add_subparsers(dest="cmd", required=True)
    s = sub.add_parser("status"); s.add_argument("--branch"); s.set_defaults(fn=cmd_status)
    h = sub.add_parser("handprobe"); h.add_argument("--title", required=True); h.add_argument("--body-file", required=True)
    h.set_defaults(fn=cmd_handprobe)
    p = sub.add_parser("pr"); p.add_argument("--title", required=True); p.add_argument("--body-file", required=True)
    p.add_argument("--base", default="main"); p.add_argument("--draft", action="store_true"); p.set_defaults(fn=cmd_pr)
    args = ap.parse_args()
    args.fn(args)


if __name__ == "__main__":
    main()
