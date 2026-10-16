# Kompiliert die Spielschicht gegen die DLLs eines installierten Unity-Editors – ohne Unity zu starten.
# Drei Varianten: altes Input System, neues Input System, Editor-Skripte. CS0618 (veraltete API) zählt als Fehler.
#
#   powershell -File tools/unity_compile_check/check.ps1
#   $env:UNITY_EDITOR_DATA = "D:\Unity\6000.x\Editor\Data"; powershell -File tools/unity_compile_check/check.ps1
#
# Ersetzt keinen Test im Editor: Laufzeitfehler (fehlende Shader, Layout, Eingabe) findet nur ein Spieltest.
$ErrorActionPreference = "Stop"
$here = Split-Path -Parent $MyInvocation.MyCommand.Path
$failed = 0
foreach ($p in "Game.Legacy", "Game.InputSystem", "Editor") {
    Write-Host "=== $p"
    dotnet build (Join-Path $here "$p.csproj") -nologo -v q
    if ($LASTEXITCODE -ne 0) { $failed++ }
}
if ($failed -gt 0) { Write-Host "$failed Variante(n) mit Fehlern"; exit 1 }
Write-Host "Unity-Kompilierung: alle drei Varianten in Ordnung"
