<#
Arrete l'application erpWpf.Wpf demarree via demarrer-application.ps1 (ou lancee depuis src/erpWpf.Wpf/bin).
#>

$ErrorActionPreference = "Stop"

$racineDepot = $PSScriptRoot
$fichierPid = Join-Path $racineDepot ".erpwpf-app.pid"
$dossierBinaires = Join-Path $racineDepot "src\erpWpf.Wpf\bin"

$identifiantsAArreter = @()

if (Test-Path $fichierPid) {
    $identifiantProcessus = Get-Content $fichierPid -ErrorAction SilentlyContinue
    if ($identifiantProcessus -and (Get-Process -Id $identifiantProcessus -ErrorAction SilentlyContinue)) {
        $identifiantsAArreter += $identifiantProcessus
    }
    Remove-Item $fichierPid -Force
}

# "dotnet run" demarre erpWpf.Wpf.exe comme processus descendant ; selon la profondeur de
# l'arbre, "taskkill /T" ne le retrouve pas toujours (reparentage Windows). On le recherche
# donc aussi directement par son chemin d'execution.
$identifiantsAArreter += Get-CimInstance Win32_Process -Filter "Name = 'erpWpf.Wpf.exe'" |
    Where-Object { $_.ExecutablePath -like "$dossierBinaires*" } |
    Select-Object -ExpandProperty ProcessId

$identifiantsAArreter = $identifiantsAArreter | Select-Object -Unique

if (-not $identifiantsAArreter) {
    Write-Host "Aucune application erpWpf en cours."
    exit 0
}

foreach ($identifiant in $identifiantsAArreter) {
    # Un identifiant precedent a pu deja entrainer l'arret de celui-ci (arbre de processus commun).
    if (-not (Get-Process -Id $identifiant -ErrorAction SilentlyContinue)) {
        continue
    }

    Write-Host "Arret de erpWpf (PID $identifiant)..."
    taskkill /PID $identifiant /T /F | Out-Null
}

Write-Host "erpWpf arrete."
