# erpWeb.Core — couche métier

Aucune dépendance vers Infrastructure, la présentation WPF (`erpWpf.Wpf`) ou les assemblages WPF (`PresentationFramework`, `WindowsBase`) : vérifié par `tests/erpWeb.UnitTests/Architecture/ArchitectureTests.cs`.

## Organisation par module

`Communs/`, `Autorisation/`, `Utilisateurs/`, `Audit/`, `Documents/`, `Parametres/`, `Email/`, `TableauDeBord/`, `Clients/`, `Echantillons/`, `Tournees/`, `Factures/`, `PlanningAnalyses/`. Un module contient :

- l'entité (`Document.cs`), les DTOs (`Dtos.cs`), les validateurs FluentValidation
- l'interface et le service (`IServiceDocuments`, `ServiceDocuments`)
- la configuration Mapster `ConfigurationMapping<Module> : IRegister`
- les options typées éventuelles (`OptionsStockage`, avec une constante `Section`)

Modèle de référence pour un nouveau module : `Documents/` (CRUD simple) ou `Clients/` (validation, normalisation).

## Règles

- **Données** : uniquement via `IAppDbContext` (DbSet + `SaveChangesAsync`). Une nouvelle entité ajoute son `DbSet` dans `IAppDbContext`.
- **Audit** : les entités tracées héritent de `EntiteAuditable` ; ne jamais renseigner `DateCreation`/`CreePar`/`DateModification`/`ModifiePar` à la main.
- **Services** : valider explicitement avec `IValidator<T>`, retourner `ResultatOperation` / `ResultatOperation<T>` pour les erreurs métier (pas d'exception), `CancellationToken jetonAnnulation` sur chaque méthode asynchrone. Les services sont scoped : la présentation ouvre une portée par opération.
- **Dates** : `TimeProvider` injecté (`GetUtcNow().UtcDateTime`), jamais `DateTime.Now`. Utiliser `DateTime` en base (les tests unitaires s'exécutent sur SQLite, qui ne trie pas les `DateTimeOffset`).
- **Système externe** (fichiers, SMTP, HTTP, Excel) : définir une interface ici, l'implémenter dans Infrastructure.
- **Mapping** : `IMapper` injecté ; jamais `TypeAdapterConfig.GlobalSettings`.
- **DI** : enregistrer les services dans `DependencyInjection.AddCore` (Scoped). Les widgets `IWidgetTableauDeBord` sont découverts automatiquement.
- **Permissions** : ajouter les constantes dans `Autorisation/Permissions.cs`, les inclure dans `Toutes` et dans `RolesApplication.PermissionsParRole`. Elles sont portées par les claims des rôles et lues à la connexion (`IServiceAuthentification`).
- **Neutralité vis-à-vis de la présentation** : pas d'URL, de classe CSS ni de type WPF. Les widgets renvoient des destinations de navigation (`Communs/Destinations`) et des noms d'icônes neutres (`TableauDeBord/Icones`).
- **Identity** : `UserManager`/`RoleManager` (Microsoft.Extensions.Identity.Stores) sans dépendance à ASP.NET Core ; pas de `SignInManager` ni de Data Protection (jetons par `AddJetonsIdentite`).
