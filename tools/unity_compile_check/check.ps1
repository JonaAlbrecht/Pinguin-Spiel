# Prüft die Spielschicht gegen einen installierten Unity-Editor – ohne Unity zu starten.
#  1. Paketliste: Jedes Paket in Packages/manifest.json muss der Editor kennen (eingebaut, mitgeliefert oder
#     in seiner Registry-Liste). Sonst bricht Unity beim Öffnen mit „Package … cannot be found“ ab – so geschehen
#     mit com.unity.modules.textrendering, das Unity 6.4 nicht mehr als Paket führt (Spieltest 17.10.2026).
#  2. Kompilierung in drei Varianten: altes Input System, neues Input System, Editor-Skripte.
#     CS0618 (veraltete API) zählt als Fehler.
#
#   powershell -ExecutionPolicy Bypass -File tools/unity_compile_check/check.ps1
#   $env:UNITY_EDITOR_DATA = "D:\Unity\6000.x\Editor\Data"; powershell -ExecutionPolicy Bypass -File tools/unity_compile_check/check.ps1
#
# Ersetzt keinen Test im Editor: Laufzeitfehler (fehlende Shader, Layout, Eingabe) findet nur ein Spieltest.
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$root = Resolve-Path (Join-Path $here "..\..")
$editorData = $env:UNITY_EDITOR_DATA
if (-not $editorData) { $editorData = "C:\Program Files\Unity\Hub\Editor\6000.4.10f1\Editor\Data" }
$failed = 0

# ---------- 1. Paketliste ----------
Write-Host "=== Paketliste"
$pm = Join-Path $editorData "Resources\PackageManager"
$known = @{}
Get-ChildItem (Join-Path $pm "BuiltInPackages") -Directory | ForEach-Object { $known[$_.Name] = "eingebaut" }
Get-ChildItem (Join-Path $pm "Editor") -Filter *.tgz | ForEach-Object { $known[($_.BaseName -replace '-\d[^-]*$', '')] = "mitgeliefert" }
$registry = (Get-Content (Join-Path $pm "Editor\manifest.json") -Raw -Encoding UTF8 | ConvertFrom-Json).packages
$registry.PSObject.Properties.Name | ForEach-Object { if (-not $known.ContainsKey($_)) { $known[$_] = "Registry" } }
$manifest = Get-Content (Join-Path $root "unity\PinguinKolonie\Packages\manifest.json") -Raw -Encoding UTF8 | ConvertFrom-Json
foreach ($p in $manifest.dependencies.PSObject.Properties) {
    if ($p.Value -like "file:*") {
        $local = Join-Path (Join-Path $root "unity\PinguinKolonie\Packages") ($p.Value.Substring(5))
        if (-not (Test-Path (Join-Path $local "package.json"))) { Write-Host "  FEHLT  $($p.Name): lokales Paket $local nicht gefunden"; $failed++ }
    }
    elseif (-not $known.ContainsKey($p.Name)) { Write-Host "  FEHLT  $($p.Name) $($p.Value): kennt dieser Editor nicht"; $failed++ }
}
if ($failed -eq 0) { Write-Host "  alle $(@($manifest.dependencies.PSObject.Properties).Count) Pakete bekannt" }

# ---------- 2. Kompilierung ----------
foreach ($p in "Game.Legacy", "Game.InputSystem", "Editor") {
    Write-Host "=== $p"
    dotnet build (Join-Path $here "$p.csproj") -nologo -v q
    if ($LASTEXITCODE -ne 0) { $failed++ }
}
if ($failed -gt 0) { Write-Host "$failed Prüfung(en) mit Fehlern"; exit 1 }
Write-Host "Unity-Prüfung: Paketliste und alle drei Varianten in Ordnung"
