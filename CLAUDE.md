# erpWpf — instructions pour Claude Code

Socle ERP PME en client lourd **WPF (.NET 10, MVVM)**, connecté directement à SQL Server par EF Core (tests unitaires sur SQLite en mémoire), déployé sur un PC ou une VM Windows. Spécification complète : [docs/prompt-generation-socle-erp.md](docs/prompt-generation-socle-erp.md).

## Commandes

```bash
dotnet build erpWpf.sln
dotnet test erpWpf.sln
dotnet format erpWpf.sln --verify-no-changes
dotnet ef migrations add <NomEnFrancais> -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf -o Donnees/Migrations
dotnet ef database update -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf
dotnet run --project src/erpWpf.Wpf
dotnet publish src/erpWpf.Wpf -c Release -r win-x64 --self-contained
```

## Architecture

- `src/erpWeb.Core` : entités, interfaces, services, DTOs, validateurs, widgets. Aucune dépendance vers les autres projets ni vers WPF ; seul package d'accès aux données autorisé : `Microsoft.EntityFrameworkCore`
- `src/erpWeb.Infrastructure` → Core : `AppDbContext` (implémente `IAppDbContext`), configurations EF, migrations, email, stockage, exports
- `src/erpWpf.Wpf` → Core + Infrastructure (Infrastructure uniquement dans `App.xaml.cs` et `Hote/`) : ViewModels, vues XAML, navigation, session, ressources
- Pas de repositories : les services utilisent `IAppDbContext`, jamais `AppDbContext`
- Les ViewModels n'appellent les services de Core que par `IExecuteurOperations` : **une portée d'injection (donc un `DbContext`) par opération**, l'équivalent d'une requête HTTP

## Règles de conception (bloquantes)

- **SRP** : un service par cas d'usage ; ViewModels minces (commande → service → état affiché), aucune logique dans le code-behind
- **OCP** : extension par interfaces et enregistrements en DI (`IWidgetTableauDeBord`, `DefinitionPage`, `EntreeMenu`, intercepteurs)
- **LSP** : aucune `NotImplementedException` dans une implémentation
- **ISP** : interfaces petites et ciblées ; une classe d'options par section
- **DIP** : interfaces dans Core (ou dans `erpWpf.Wpf/Services` pour les services d'interface), implémentations dans Infrastructure, injection par constructeur
- Interdits : état statique, singleton maison, Service Locator (`IServiceProvider` hors composition root), `new` de services, `DateTime.Now` (utiliser `TimeProvider`), `async void` hors gestionnaire XAML, `.Result`/`.Wait()`, `MessageBox` dans un ViewModel, optimisation sans mesure, abréviations, copier-coller entre modules

## Conventions

- Nommage **en français**, sans accents ni abréviations (`ServiceUtilisateurs`, `DateCreation`) ; termes du framework conservés (`ViewModel`, `Command`, `DbContext`, suffixe `Async`)
- Nullable activé, avertissements traités comme erreurs, `async`/`await` + `CancellationToken` sur les I/O
- CommunityToolkit.Mvvm : propriétés partielles `[ObservableProperty] public partial …`, commandes `[RelayCommand]`

## GitHub Flow

- Jamais de commit sur `main` : travailler sur `<type>/<description>` (`fonctionnalite/`, `correctif/`, `refactorisation/`, `documentation/`, `technique/`)
- Commits Conventional Commits en français : `feat(module): description`
- Une migration par PR maximum ; ne jamais modifier une migration déjà fusionnée dans `main`

## Outillage

- Instructions détaillées par couche : `src/erpWeb.Core/CLAUDE.md`, `src/erpWeb.Infrastructure/CLAUDE.md`, `src/erpWpf.Wpf/CLAUDE.md`, `tests/CLAUDE.md`
- Skills : `/demarrer-branche` avant toute évolution, `/nouveau-module`, `/nouveau-widget`, `/preparer-pr` avant d'ouvrir une Pull Request
- Agents : `revue-conception` (relecture SOLID/STUPID et MVVM, lecture seule), `redacteur-tests`, `expert-migrations`
- Hooks : un retour de `verifier-regles` ou `verifier-build` est bloquant, corriger la cause plutôt que contourner la règle
- Consulter la documentation Microsoft Learn (MCP) avant d'utiliser une API .NET 10 / EF Core 10 / WPF récente
