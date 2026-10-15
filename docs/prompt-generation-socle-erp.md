# Prompt Claude Code — Génération du socle applicatif ERP PME (client WPF)

## Rôle et objectif

Tu es chargé de générer le socle technique d'une application ERP pour PME, en client lourd **WPF (C#, MVVM)** connecté directement à SQL Server, déployé sur un PC ou une VM Windows. Le socle doit être fonctionnel, buildable dès la génération, et respecter l'architecture, les choix technologiques et les principes de conception décrits ci-dessous.

> Le socle existant est issu d'une version ASP.NET Core MVC (`erpWeb`) : les projets `erpWeb.Core` et `erpWeb.Infrastructure` et les migrations EF Core sont conservés ; la présentation est `erpWpf.Wpf`.

**Règle générale** : sauf indication contraire explicite, utilise systématiquement la **solution par défaut** indiquée pour chaque dépendance ou module. Les **alternatives** mentionnées sont fournies à titre indicatif (contexte, avantages/inconvénients) et ne doivent être implémentées que sur demande explicite.

**Versionnement de la génération** : initialise le dépôt sur `main` avec un premier commit contenant uniquement `.gitignore`, `.gitattributes` et `README.md`. Génère ensuite le socle sur une branche `fonctionnalite/…` ou `refactorisation/…`, en commits atomiques par étape (solution et projets, accès aux données, authentification, présentation, modules, tests, CI), en respectant les règles de la section « Gestion de version — GitHub Flow ». Ne fusionne pas dans `main` : la branche est destinée à une Pull Request.

## Stack technique de base

| Élément | Choix |
| --- | --- |
| Framework | **.NET 10 (LTS)**, `net10.0` (Core, Infrastructure) et `net10.0-windows` (WPF) |
| Pattern d'architecture | MVVM + architecture en couches (Core / Infrastructure / Wpf) |
| Présentation | WPF (XAML), CommunityToolkit.Mvvm, hôte générique .NET |
| Base de données | Entity Framework Core + SQL Server (LocalDB en développement), connexion directe du poste |
| Déploiement | Publication autonome `win-x64` + bundle de migrations EF Core |

## Dépendances techniques (packages NuGet)

### Accès aux données / ORM

- **Défaut** : `Microsoft.EntityFrameworkCore.SqlServer` + `Microsoft.EntityFrameworkCore.Tools` — ORM standard Microsoft, migrations Code-First, intégration avec les stores Identity
  - Base locale : SQL Server LocalDB, `Server=(localdb)\MSSQLLocalDB;Database=erpWeb;Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True`
  - Production : sécurité intégrée Windows de préférence ; le compte SQL des postes est limité à `db_datareader` / `db_datawriter`
- **Pas de repositories** : les services métier accèdent aux données via l'interface `IAppDbContext` (définie dans Core, implémentée par `AppDbContext` dans Infrastructure)
  - `IAppDbContext` expose uniquement les `DbSet<T>` nécessaires et `SaveChangesAsync(CancellationToken)` ; elle n'expose ni `Database`, ni `ChangeTracker`
  - Core référence uniquement le package `Microsoft.EntityFrameworkCore` (pour `DbSet<T>` et LINQ asynchrone), jamais le provider SQL Server
- **Unité de travail** : une portée d'injection, donc un `DbContext`, par opération déclenchée par l'interface (`IExecuteurOperations`) ; jamais un `DbContext` conservé par un écran

### Identité (comptes, rôles, claims)

- **Défaut** : `Microsoft.Extensions.Identity.Stores` (Core) + `Microsoft.AspNetCore.Identity.EntityFrameworkCore` (stores EF, Infrastructure) — `UserManager` / `RoleManager` sans hébergement web
  - Connexion par un service de Core (`IServiceAuthentification` : compte actif, verrouillage après échecs, permissions issues des claims de rôle), pas de `SignInManager`
  - Jetons de réinitialisation TOTP (`EmailTokenProvider`) : pas de Data Protection d'ASP.NET Core

### Présentation

- **Défaut** : `CommunityToolkit.Mvvm` — `ObservableObject`, propriétés partielles `[ObservableProperty]`, `[RelayCommand]` asynchrones
- `Microsoft.Extensions.Hosting` — configuration (`appsettings*.json`, secrets utilisateur en Development, variables d'environnement), injection de dépendances, journalisation (journal d'événements Windows)
- `OxyPlot.Wpf` — graphiques du tableau de bord
- Icônes : police système Segoe Fluent Icons (aucun package)
- *Alternative* : `LiveChartsCore.SkiaSharpView.WPF` (graphiques animés), bibliothèque de contrôles tierce (MahApps, ModernWpf) — sur demande uniquement

### Mapping objets / DTO

- **Défaut** : `Mapster` + `Mapster.DependencyInjection` — licence MIT, rapide à l'exécution, syntaxe concise ; configuration par module (une classe `IRegister` par module)
- *Alternative écartée* : `AutoMapper` — la dernière version MIT (14.0.0) présente une vulnérabilité de gravité élevée (CVE-2026-32933, déni de service) et les versions corrigées (15.1.1+) sont sous licence commerciale

### Validation

- **Défaut** : `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` — règles de validation complexes et testables, découplées des modèles ; validateurs appelés explicitement dans la couche Core, erreurs restituées par `ResultatOperation` et affichées par le ViewModel

### Logging

- **Défaut** : `Microsoft.Extensions.Logging` (natif, fournisseur EventLog sur Windows)
- *Alternative* : `Serilog` — logs structurés, fichiers par poste, Seq

### Envoi d'emails

- **Défaut** : `MailKit` — envoi SMTP fiable, utilisé uniquement via l'interface `IServiceEmail` (définie dans Core, implémentée dans Infrastructure)

### Impression et documents

- **Défaut** : `FlowDocument` + `PrintDialog` (natifs WPF) — aperçu à l'écran et impression du même document
- *Alternative* : `QuestPDF` — génération de PDF archivables (licence Community sous conditions de CA)

### Export / import Excel

- **Défaut** : `ClosedXML` — API simple, licence MIT, sans limite d'usage

### Tests

- `xUnit` + `Moq` — standard pour les tests unitaires .NET
- `Microsoft.EntityFrameworkCore.Sqlite` en mémoire (`DataSource=:memory:`) — tests unitaires des services s'appuyant sur `IAppDbContext` (préférable au mock des `DbSet<T>`)
- ViewModels testés sans interface graphique : services simulés et exécuteur d'opérations de test
- SQL Server pour les tests d'intégration — composition réelle de l'application et base temporaire par exécution avec les migrations réelles : LocalDB en local et en CI Windows (variable `ERPWEB_TESTS_SQLSERVER` pour un autre serveur)
- `Bogus` — génération de données de test réalistes (jeux de démo)
- `Microsoft.Extensions.TimeProvider.Testing` — horloge contrôlée (minuteries de session et de notifications)
- `NetArchTest.Rules` — tests d'architecture vérifiant automatiquement les dépendances entre couches et les règles MVVM

## Modules transverses du socle

### Authentification & autorisation

- **Défaut** : écran de connexion applicatif sur les comptes Identity en base ; session du poste (`ISessionUtilisateur`) fournissant l'utilisateur courant à l'audit
- Permissions fines par module/action portées par les claims des rôles ; contrôle par `IAutorisation` dans la navigation (page refusée → écran « Accès refusé »), les commandes (`CanExecute`) et l'affichage
- Surveillance de session : relecture périodique (1 min) — compte désactivé, verrouillé ou mot de passe réinitialisé → retour à l'écran de connexion ; permissions actualisées
- *Alternative* : authentification Windows (utilisateur du domaine associé à un compte ERP) — sur demande

### Gestion des utilisateurs

- **Défaut** : écrans d'administration (rôle `Admin`) basés sur `UserManager` / `RoleManager` : liste, création, modification, activation/désactivation des comptes, affectation des rôles, réinitialisation du mot de passe

### Tableau de bord

- **Défaut** : écran de widgets modulaires, chaque widget rendu par un `DataTemplate` selon le type de ses données (indicateur, graphique OxyPlot, liste, raccourcis), rafraîchissement à la demande
- Chaque widget implémente l'interface `IWidgetTableauDeBord` et est découvert via l'injection de dépendances (ajout d'un widget sans modifier l'écran) ; liens exprimés en destinations de navigation et icônes en noms neutres (aucune URL ni classe CSS dans Core)

### Navigation

- **Défaut** : destinations `Module/Action/parametre` (constantes `Destinations` dans Core) ; chaque module enregistre ses pages (`DefinitionPage`), actions (`DefinitionAction`) et entrées de menu (`EntreeMenu`) dans l'injection de dépendances ; un écran peut refuser d'être quitté (modifications non enregistrées)

### Journal d'audit

- **Défaut** : interception du `SaveChanges` d'EF Core (`IntercepteurAudit` héritant de `SaveChangesInterceptor`) pour tracer les créations, modifications et suppressions
- Table `JournalAudit` : entité, identifiant, action, utilisateur, date, valeurs avant/après (JSON)
- Consultation : grille paginée, filtrée et triée **côté serveur** (liste blanche des colonnes triables), sans plafond sur le nombre d'entrées consultables ; export Excel

### Gestion documentaire

- **Défaut** : stockage des fichiers sur disque (dossier local du poste ou partage réseau UNC en multi-postes, chemin référencé en base) via l'interface `IStockageFichiers` (implémentation `StockageFichiersLocal`), remplaçable par un stockage cloud sans modification du code appelant
- Table `Documents` liée de manière polymorphe aux entités (colonnes `TypeEntite` + `IdEntite`)

### Paramétrage général

- **Défaut** : table `Parametres` clé/valeur en base (dernière source de configuration) + pattern `IOptions<T>` pour la configuration typée en code
  - Une classe d'options par section (`OptionsSmtp`, `OptionsStockage`), jamais de lecture statique de la configuration

## Structure de projet attendue

```text
erpWpf.sln
├── src/
│   ├── erpWeb.Core/             # Entités, règles métier, interfaces (dont IAppDbContext), services applicatifs, DTOs, validateurs, widgets
│   ├── erpWeb.Infrastructure/   # AppDbContext, configurations et migrations EF Core, Identity, envoi d'emails, stockage fichiers, exports
│   └── erpWpf.Wpf/              # App.xaml, Hote/ (composition root), Securite/, Navigation/, Services/, ViewModels/, Views/, Fenetres/, Ressources/
└── tests/
    ├── erpWeb.UnitTests/
    └── erpWeb.IntegrationTests/
```

Dépendances entre projets :

- `Core` → aucune dépendance vers les autres projets ni vers WPF (seul package d'accès aux données autorisé : `Microsoft.EntityFrameworkCore`)
- `Infrastructure` → `Core`
- `Wpf` → `Core` + `Infrastructure` (uniquement dans `App.xaml.cs` et `Hote/` pour la composition)

## Principes de conception

### Principes SOLID (à appliquer)

| Principe | Application attendue dans le socle |
| --- | --- |
| **S** — Single Responsibility | Un service par cas d'usage ou agrégat (`ServiceFacturation`, pas `ServiceErp`). ViewModels limités à : exposer l'état, déclencher un service, présenter le résultat ; code-behind vide. Validation (validateurs FluentValidation), mapping (une configuration Mapster `IRegister` par module) et accès aux données (`IAppDbContext`) séparés. |
| **O** — Open/Closed | Extension sans modification : widgets via `IWidgetTableauDeBord`, écrans et menu via `DefinitionPage` / `EntreeMenu`, rendu via `DataTemplate`, audit via `SaveChangesInterceptor`, stockage via `IStockageFichiers`. L'ajout d'un module ne doit pas modifier le code existant du socle. |
| **L** — Liskov Substitution | Toute implémentation d'une interface respecte son contrat, sans `NotImplementedException` ni comportement restreint. Les implémentations de test doivent être substituables sans modifier le code appelant. |
| **I** — Interface Segregation | Interfaces petites et ciblées (`IStockageFichiers`, `IServiceEmail`, `ILectureJournalAudit`, `IServiceDialogues`, `IAutorisation`). `IAppDbContext` limitée aux `DbSet<T>` et à `SaveChangesAsync`. Options typées découpées par section. |
| **D** — Dependency Inversion | Les interfaces métier sont définies dans `Core` et implémentées dans `Infrastructure` ; les services d'interface (dialogues, fichiers, impression) sont derrière des interfaces de la présentation. Les services dépendent de `IAppDbContext`, jamais de `AppDbContext`. `Core` ne référence ni le provider SQL Server, ni MailKit, ni WPF, et n'accède pas directement au système de fichiers. Toutes les dépendances sont injectées par constructeur. |

### Anti-patterns STUPID (à proscrire)

| Anti-pattern | Règle |
| --- | --- |
| **S** — Singleton | Aucune classe statique contenant un état, aucun singleton « maison » (`Instance`). Durées de vie gérées par le conteneur DI : `Scoped` pour `AppDbContext` et les services métier (une portée par opération), `Transient` pour les ViewModels, `Singleton` pour la session et les services sans état. Pas de Service Locator (injection de `IServiceProvider` interdite hors composition root). |
| **T** — Tight Coupling | Aucune instanciation directe (`new`) de services ou d'infrastructure dans le code métier ou les ViewModels. `Wpf` ne référence `Infrastructure` que dans la composition root. |
| **U** — Untestability | Aucun accès direct à `DateTime.Now` (utiliser `TimeProvider`), au système de fichiers, au réseau ou à un type WPF depuis `Core` ; aucun `MessageBox`, contrôle ou dialogue système dans un ViewModel. Chaque service métier et chaque ViewModel doit être testable unitairement. |
| **P** — Premature Optimization | Pas de repositories, CQRS, MediatR, microservices, cache généralisé ni SQL brut sans besoin mesuré. Privilégier un code simple et lisible ; toute optimisation doit être justifiée. |
| **I** — Indescriptive Naming | Noms explicites **en français**, sans accents ni abréviations (`ServiceClient`, `montantTotal`, `DateEcheance` ; pas `SrvCli`, `mnt`). Exceptions : types et termes imposés par le framework (`AppDbContext`, `IAppDbContext`, `ViewModel`, `Command`, suffixe `Async`). Conventions .NET : PascalCase pour types et membres publics, `_camelCase` pour les champs privés, préfixe `I` pour les interfaces. |
| **D** — Duplication | Logique commune factorisée : classe de base `EntiteAuditable`, `ViewModelBase` et `GrillePagineeViewModel`, styles et contrôles XAML partagés (`EnTetePage`, `BarrePagination`), configuration EF Core via `IEntityTypeConfiguration<T>`. Pas de copier-coller entre modules. |

### Contraintes de code

- C# avec nullable reference types activés et avertissements traités comme erreurs (`TreatWarningsAsErrors`)
- `async`/`await` systématique sur les opérations I/O, avec propagation du `CancellationToken` ; pas d'`async void` hors gestionnaire d'événement XAML ; pas de `.Result`/`.Wait()`
- Aucune logique métier dans les ViewModels ni dans le code-behind (délégation vers les services de la couche Core)
- Injection de dépendances par constructeur, via les interfaces définies dans Core ou dans la présentation

## Gestion de version — GitHub Flow

### Règles du flux

1. `main` est **toujours déployable** : aucun commit direct, toute modification passe par une Pull Request
2. Chaque évolution part d'une branche courte créée depuis `main` à jour
3. Commits petits et atomiques ; la branche est poussée régulièrement
4. Ouverture d'une Pull Request dès que possible (en brouillon si le travail est en cours)
5. Fusion dans `main` uniquement si la CI est verte et après au moins une relecture approuvée
6. Fusion en **squash merge**, puis suppression de la branche
7. Livraison depuis `main` après fusion : `publier-application.ps1` (application autonome + bundle de migrations) ; la diffusion sur les postes est hors du périmètre du socle

### Nommage des branches

Format : `<type>/<description-courte-en-kebab-case>`, sans accents

| Type | Usage | Exemple |
| --- | --- | --- |
| `fonctionnalite/` | Nouvelle fonctionnalité | `fonctionnalite/gestion-utilisateurs` |
| `correctif/` | Correction de bug | `correctif/calcul-montant-tva` |
| `refactorisation/` | Refactorisation sans changement fonctionnel | `refactorisation/service-facturation` |
| `documentation/` | Documentation uniquement | `documentation/readme-lancement` |
| `technique/` | CI, dépendances, configuration | `technique/mise-a-jour-ef-core` |

### Messages de commit

- Convention [Conventional Commits](https://www.conventionalcommits.org/fr/), description en français : `feat(utilisateurs): ajouter la désactivation d'un compte`
- Types autorisés : `feat`, `fix`, `refactor`, `docs`, `test`, `chore`, `ci`

### Migrations EF Core

- Au plus une migration par Pull Request, nommée en français (`AjoutTableDocuments`), projet de démarrage `src/erpWpf.Wpf`
- Avant fusion, mettre la branche à jour depuis `main` ; en cas de conflit sur `AppDbContextModelSnapshot`, supprimer la migration de la branche et la régénérer
- Aucune modification d'une migration déjà fusionnée dans `main`
- En production, migrations appliquées par un administrateur via `efbundle.exe`, jamais au démarrage d'un poste

### Contrôles automatisés

| Règle du prompt | Contrôle bloquant avant fusion |
| --- | --- |
| STUPID — Untestability | La CI exécute les tests unitaires, d'intégration et d'architecture |
| Contraintes de code — `TreatWarningsAsErrors` | Build CI en échec en cas d'avertissement |
| STUPID — Indescriptive Naming | Conventions `.editorconfig` vérifiées par `dotnet format --verify-no-changes` |
| Dépendances entre couches, règles MVVM | Tests d'architecture NetArchTest |

## Livrables attendus de la génération

1. Solution .NET 10 buildable, structurée en 3 projets (Core, Infrastructure, Wpf) + 2 projets de tests
2. Migrations EF Core incluant les tables du socle (tables Identity `AspNetUsers`/`AspNetRoles`…, `JournalAudit`, `Documents`, `Parametres`) et des modules
3. Authentification fonctionnelle (écran de connexion, inscription) avec au moins un utilisateur admin seedé
4. Fenêtre principale fonctionnelle (menu filtré par permissions, zone de page, bandeau de messages) et tableau de bord
5. `README.md` avec les instructions de lancement (prérequis, `dotnet ef database update`, `dotnet run`), de déploiement sur PC/VM (publication autonome, bundle de migrations, droits SQL, dossier des documents) et une section « Principes de conception » résumant les règles SOLID/STUPID appliquées
6. `appsettings.json`, `appsettings.Development.json` (LocalDB, migrations au démarrage) et `appsettings.Production.json` (modèle de connexion au serveur)
7. Tests d'architecture (NetArchTest) vérifiant que `Core` ne dépend ni d'`Infrastructure`, ni de la présentation, ni de WPF, ni des providers SQL Server et SQLite, que les ViewModels n'accèdent ni aux données ni aux types graphiques, et que seul `AppDbContext` implémente `IAppDbContext`
8. Au moins un test unitaire par service et par ViewModel d'écran du socle, démontrant leur testabilité
9. Dépôt Git initialisé avec `.gitignore` (`dotnet new gitignore`) et `.gitattributes` (fins de ligne normalisées)
10. Workflow GitHub Actions `.github/workflows/ci.yml` sur `windows-latest`, déclenché sur `pull_request` et `push` vers `main` : LocalDB → `dotnet restore` → `dotnet build` (avertissements = erreurs) → `dotnet format --verify-no-changes` → `dotnet test`
11. Modèle de Pull Request `.github/pull_request_template.md` : description, type de changement, migration incluse (oui/non), checklist (tests ajoutés, principes SOLID respectés, aucun anti-pattern STUPID, permissions des écrans, CI verte)
12. `.editorconfig` imposant les conventions de nommage et de style (vérifiées par `dotnet format` en CI)
13. `CONTRIBUTING.md` décrivant le GitHub Flow, le nommage des branches et les conventions de commit
14. Section « Protection de la branche main » dans le `README.md` : paramètres à activer sur GitHub (PR obligatoire, 1 approbation, CI requise, squash merge uniquement, suppression automatique des branches), avec la commande `gh api` équivalente
15. Scripts `demarrer-application.ps1`, `arreter-application.ps1`, `publier-application.ps1`
16. Outillage Claude Code :
    - `CLAUDE.md` à la racine et dans chaque projet (`src/erpWeb.Core/`, `src/erpWeb.Infrastructure/`, `src/erpWpf.Wpf/`, `tests/`)
    - `.claude/settings.json` : permissions et hooks (protection de `main`, Conventional Commits, migrations fusionnées, détection des anti-patterns STUPID et des écarts MVVM, formatage, build et tests d'architecture en fin de tour)
    - Agents `.claude/agents/` : `revue-conception` (lecture seule), `redacteur-tests`, `expert-migrations`
    - Skills `.claude/skills/` : `nouveau-module`, `nouveau-widget`, `demarrer-branche`, `preparer-pr`
