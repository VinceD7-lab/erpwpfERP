<#
Produit le livrable de deploiement dans artefacts/ :
  - artefacts/erpWpf/      application WPF autonome (self-contained win-x64, aucun runtime .NET requis sur le poste)
  - artefacts/efbundle.exe migrations EF Core a appliquer par un administrateur sur le serveur SQL
Usage : .\publier-application.ps1 [-Runtime win-x64] [-SansBundle]
#>

param(
    [string] $Runtime = "win-x64",
    [switch] $SansBundle
)

$ErrorActionPreference = "Stop"

$racineDepot = $PSScriptRoot
$dossierArtefacts = Join-Path $racineDepot "artefacts"
$dossierApplication = Join-Path $dossierArtefacts "erpWpf"

if (Test-Path $dossierApplication) {
    Remove-Item $dossierApplication -Recurse -Force
}
New-Item -ItemType Directory -Force $dossierArtefacts | Out-Null

Write-Host "Publication de l'application ($Runtime, autonome)..."
dotnet publish (Join-Path $racineDepot "src/erpWpf.Wpf") -c Release -r $Runtime --self-contained -o $dossierApplication -nologo
if ($LASTEXITCODE -ne 0) { throw "Echec de la publication." }

# Les reglages de developpement ne partent pas en production.
Remove-Item (Join-Path $dossierApplication "appsettings.Development.json") -ErrorAction SilentlyContinue

if (-not $SansBundle) {
    Write-Host "Generation du bundle de migrations EF Core..."
    dotnet ef migrations bundle -p (Join-Path $racineDepot "src/erpWeb.Infrastructure") -s (Join-Path $racineDepot "src/erpWpf.Wpf") `
        -r $Runtime --self-contained --force -o (Join-Path $dossierArtefacts "efbundle.exe")
    if ($LASTEXITCODE -ne 0) { throw "Echec de la generation du bundle de migrations." }
}

Write-Host ""
Write-Host "Livrable pret dans $dossierArtefacts"
Write-Host "  1. Base : efbundle.exe --connection ""Server=...;Database=erpWeb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"""
Write-Host "  2. Poste : copier artefacts\erpWpf, renseigner appsettings.Production.json (chaine de connexion, dossier des documents)"
Write-Host "  3. Lancer erpWpf.Wpf.exe (environnement Production par defaut)"
