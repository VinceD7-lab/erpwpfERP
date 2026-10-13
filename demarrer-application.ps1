<#
Demarre l'application WPF erpWpf.Wpf en developpement (dotnet run, environnement Development
via Properties/launchSettings.json) et enregistre son PID pour arreter-application.ps1.
#>

$ErrorActionPreference = "Stop"

$racineDepot = $PSScriptRoot
$cheminProjet = Join-Path $racineDepot "src/erpWpf.Wpf"
$fichierPid = Join-Path $racineDepot ".erpwpf-app.pid"
$fichierJournal = Join-Path $racineDepot "erpwpf-app.log"
$fichierJournalErreurs = Join-Path $racineDepot "erpwpf-app-erreurs.log"
$dossierBinaires = Join-Path $racineDepot "src\erpWpf.Wpf\bin"

$processusExistant = Get-CimInstance Win32_Process -Filter "Name = 'erpWpf.Wpf.exe'" |
    Where-Object { $_.ExecutablePath -like "$dossierBinaires*" } |
    Select-Object -First 1

if ($processusExistant) {
    Write-Host "L'application erpWpf est deja en cours d'execution (PID $($processusExistant.ProcessId))."
    exit 0
}

Write-Host "Demarrage de erpWpf.Wpf..."

$processus = Start-Process -FilePath "dotnet" `
    -ArgumentList @("run", "--project", $cheminProjet) `
    -RedirectStandardOutput $fichierJournal `
    -RedirectStandardError $fichierJournalErreurs `
    -WindowStyle Hidden `
    -PassThru

Set-Content -Path $fichierPid -Value $processus.Id

Write-Host "erpWpf demarre (PID $($processus.Id)) : la fenetre de connexion s'ouvre apres la compilation."
Write-Host "Journal : $fichierJournal"
