# erpWpf

Socle applicatif d'ERP pour PME, orienté tableau de bord. Client lourd **WPF (.NET 10, MVVM)** connecté directement à **SQL Server** par EF Core, déployé sur un PC ou une VM Windows.

> Historique : le socle était une application ASP.NET Core MVC (`erpWeb`). Les couches métier et techniques (`erpWeb.Core`, `erpWeb.Infrastructure`), le schéma de base et ses migrations sont conservés à l'identique ; seule la présentation a été remplacée par `erpWpf.Wpf`.

## Fonctionnalités

- **Tableau de bord** : widgets modulaires (utilisateurs actifs, activité sur 30 jours, derniers documents, raccourcis), filtrés selon les permissions et rafraîchissables
- **Authentification** : écran de connexion, inscription, déconnexion, verrouillage après 5 échecs, comptes désactivables ; un compte désactivé ou dont le mot de passe est réinitialisé est déconnecté en moins d'une minute
- **Autorisation** : rôles (`Admin`, `Utilisateur`) et permissions fines par module et par action (menu, écrans et commandes)
- **Clients** : liste avec recherche, édition en ligne dans la grille, fiche détaillée avec les échantillons du client, formulaire complet
- **Échantillons** : résultats d'analyse paginés côté serveur, filtres (période de tournée, client, statut), regroupement par tournée et client
- **Factures** : génération des factures manquantes, grille paginée et filtrable, aperçu et impression
- **Planning des analyses** : grille client × jour du mois, types d'analyse A à D sur les jours ouvrés
- **Utilisateurs** : liste, création, modification, activation/désactivation, rôles, réinitialisation du mot de passe
- **Journal d'audit** : traçabilité automatique des créations, modifications et suppressions (valeurs avant/après), export Excel
- **Documents** : dépôt, téléchargement et suppression de fichiers rattachés à n'importe quelle entité
- **Paramètres** : paramètres clé/valeur en base, surchargeant la configuration sans redéploiement

## Prérequis (développement)

- Windows 10/11, [SDK .NET 10](https://dotnet.microsoft.com/download/dotnet/10.0)
- Outil EF Core : `dotnet tool install --global dotnet-ef` (ou `dotnet tool update --global dotnet-ef`)
- SQL Server LocalDB (instance `MSSQLLocalDB`) : installé avec Visual Studio (charge de travail « Stockage et traitement des données ») ou via l'installateur SQL Server Express (option LocalDB) ; vérifier avec `sqllocaldb info MSSQLLocalDB`

## Démarrage en développement

```bash
# 1. Mot de passe du compte administrateur (stocké hors du dépôt)
dotnet user-secrets set "Administrateur:MotDePasse" "<MotDePasse#Solide1>" --project src/erpWpf.Wpf

# 2. Création de la base erpWeb sur LocalDB
dotnet ef database update -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf

# 3. Lancement (environnement Development via Properties/launchSettings.json)
dotnet run --project src/erpWpf.Wpf
```

Se connecter avec `admin@erpweb.local` et le mot de passe défini à l'étape 1. Les scripts `demarrer-application.ps1` / `arreter-application.ps1` lancent et arrêtent l'application en arrière-plan.

> En `Development`, les migrations sont appliquées au démarrage (`BaseDeDonnees:AppliquerMigrationsAuDemarrage`) et des clients,
> tournées et échantillons de démonstration sont générés. L'administrateur, les rôles et leurs permissions sont créés à chaque
> démarrage s'ils n'existent pas.

## Configuration

Fichiers `appsettings.json` puis `appsettings.{DOTNET_ENVIRONMENT}.json` à côté de l'exécutable, secrets utilisateur (Development), variables d'environnement (`Section__Cle`), puis table `Parametres`.

| Clé | Rôle | Valeur par défaut |
| --- | --- | --- |
| `ConnectionStrings:ParDefaut` | Connexion directe au serveur SQL | `Server=(localdb)\MSSQLLocalDB;Database=erpWeb;…` |
| `Administrateur:Email` / `NomComplet` | Compte administrateur initial | `admin@erpweb.local` |
| `Administrateur:MotDePasse` | Mot de passe initial (secrets utilisateur ou variable d'environnement, jamais versionné) | — |
| `BaseDeDonnees:AppliquerMigrationsAuDemarrage` | Migration automatique au démarrage | `true` en Development, `false` sinon |
| `Stockage:Dossier` / `TailleMaximaleOctets` | Dossier des documents (relatif → `%LOCALAPPDATA%\erpWpf`), taille maximale | `documents`, 10 Mo |
| `Facturation:*` | Tarifs par filière, TVA, échéance, devise, émetteur | voir `appsettings.json` |
| `Smtp:Hote`, `Port`, `NomUtilisateur`, `MotDePasse`… | Envoi d'emails (MailKit) ; hôte vide = emails journalisés | hôte vide |

Les paramètres saisis dans l'écran **Paramètres** (clé au format `Section:Cle`, ex. `Smtp:Hote`) sont lus en dernier et surchargent ces valeurs.

Exemples de chaîne de connexion :

```text
# Sécurité intégrée Windows (recommandé : compte du domaine ou de la VM)
Server=SRV-SQL\SQLEXPRESS;Database=erpWeb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True

# Login SQL (poste hors domaine) — préférer une variable d'environnement ConnectionStrings__ParDefaut au fichier
Server=SRV-SQL,1433;Database=erpWeb;User ID=erp_application;Password=<secret>;Encrypt=True;TrustServerCertificate=True
```

## Déploiement sur un PC ou une VM Windows

```powershell
.\publier-application.ps1          # produit artefacts\erpWpf (autonome win-x64) et artefacts\efbundle.exe
```

1. **Base de données** (une fois par version, par un administrateur ayant les droits DDL) :

   ```powershell
   .\efbundle.exe --connection "Server=SRV-SQL\SQLEXPRESS;Database=erpWeb;Integrated Security=True;Encrypt=True;TrustServerCertificate=True"
   ```

2. **Droits SQL des utilisateurs** : avec une connexion directe, le compte SQL est la vraie frontière de sécurité (les permissions de l'application filtrent les écrans, pas l'accès à la base). Donner aux postes un accès limité, sans `db_owner` :

   ```sql
   CREATE LOGIN [DOMAINE\ERP-Utilisateurs] FROM WINDOWS;
   USE erpWeb;
   CREATE USER [DOMAINE\ERP-Utilisateurs] FOR LOGIN [DOMAINE\ERP-Utilisateurs];
   ALTER ROLE db_datareader ADD MEMBER [DOMAINE\ERP-Utilisateurs];
   ALTER ROLE db_datawriter ADD MEMBER [DOMAINE\ERP-Utilisateurs];
   ```

3. **Poste** : copier `artefacts\erpWpf` (par exemple dans `C:\Program Files\erpWpf`), renseigner `appsettings.Production.json` (chaîne de connexion, `Stockage:Dossier`), créer un raccourci vers `erpWpf.Wpf.exe`. La publication est autonome : aucun runtime .NET n'est requis. L'environnement est `Production` par défaut (`DOTNET_ENVIRONMENT` pour en changer).
4. **Documents** : un seul poste → `C:\ProgramData\erpWpf\documents` (droits d'écriture pour les utilisateurs) ; plusieurs postes → partage réseau UNC commun (`\\SRV-FICHIERS\erpWpf\documents`).
5. **Reprise d'une installation erpWeb** : la base est reprise telle quelle (même schéma, mêmes migrations, comptes et mots de passe conservés). Les chemins des documents étant enregistrés relativement au dossier de stockage, copier l'arborescence `App_Data/documents` de l'ancien site vers le nouveau `Stockage:Dossier`.
6. **Premier lancement** : fournir `Administrateur:MotDePasse` une fois (variable d'environnement `Administrateur__MotDePasse`) pour créer le compte administrateur, puis la retirer.

Les erreurs non gérées affichent un identifiant et sont consignées dans le journal d'événements Windows (journal « Application », source « .NET Runtime »).

## Architecture

```text
erpWpf.sln
├── src/
│   ├── erpWeb.Core/             # Entités, interfaces, services, DTOs, validateurs, widgets (par module)
│   ├── erpWeb.Infrastructure/   # AppDbContext, configurations et migrations EF Core, audit, Identity, email, stockage, export
│   └── erpWpf.Wpf/              # App.xaml, Hote/ (composition root), ViewModels, vues XAML, navigation, session, ressources
├── tests/
│   ├── erpWeb.UnitTests/        # Services, ViewModels, navigation, intercepteur, stockage, widgets, règles d'architecture
│   └── erpWeb.IntegrationTests/ # Parcours sur la composition réelle et une base SQL Server temporaire
└── docs/prompt-generation-socle-erp.md
```

- `Core` ne dépend d'aucun autre projet ni de WPF ; `Infrastructure` → `Core` ; `Wpf` → `Core` + `Infrastructure` (uniquement dans `App.xaml.cs` et `Hote/`)
- MVVM avec CommunityToolkit.Mvvm ; hôte générique .NET (configuration, DI, journalisation)
- Une portée d'injection — donc un `DbContext` — par opération (`IExecuteurOperations`), comme une requête HTTP
- Pas de repositories : les services utilisent `IAppDbContext`, implémentée par `AppDbContext`
- Nommage en français ; versions NuGet centralisées dans `Directory.Packages.props`

### Rôles et permissions

| Permission | Admin | Utilisateur |
| --- | :---: | :---: |
| `Utilisateurs.Lire`, `Utilisateurs.Gerer` | ✓ | |
| `JournalAudit.Lire`, `JournalAudit.Exporter` | ✓ | |
| `Documents.Lire`, `Documents.Deposer` | ✓ | ✓ |
| `Documents.Supprimer` | ✓ | |
| `Parametres.Lire`, `Parametres.Modifier` | ✓ | |
| `Clients.Lire`, `Clients.Gerer` | ✓ | ✓ |
| `Echantillons.Lire` | ✓ | ✓ |
| `Factures.Lire`, `Factures.Generer` | ✓ | ✓ |
| `PlanningAnalyses.Lire`, `PlanningAnalyses.Gerer` | ✓ | ✓ |

Les permissions sont définies dans `src/erpWeb.Core/Autorisation`, synchronisées en base au démarrage et chargées à l'ouverture de session.

## Principes de conception

Le code applique les principes **SOLID** et proscrit les anti-patterns **STUPID** ; les règles vérifiables le sont automatiquement.

| Règle | Application | Vérification |
| --- | --- | --- |
| Single Responsibility | Un service par module ; ViewModels minces, code-behind vide | Relecture (agent `revue-conception`) |
| Open/Closed | Widgets découverts automatiquement ; pages, actions et menu par enregistrement DI ; audit par intercepteur | Tests `AddCore_EnregistreAutomatiquementTousLesWidgets`, `Composition_ChaqueEcranEtChaqueAction_EstResolvable` |
| Liskov / Interface Segregation | Interfaces petites (`IStockageFichiers`, `IServiceDialogues`, `IAutorisation`) | Relecture |
| Dependency Inversion | Interfaces dans Core, implémentations dans Infrastructure ; services d'interface derrière des interfaces | Tests d'architecture NetArchTest |
| Pas de Singleton / Tight coupling | Durées de vie gérées par la DI, `ValidateScopes`, aucune instanciation de service | Hook `verifier-regles`, tests d'architecture |
| Testabilité | `TimeProvider`, SQLite en mémoire, ViewModels sans type graphique | 400+ tests |
| Nommage explicite | Conventions `.editorconfig` | `dotnet format --verify-no-changes` (CI) |
| Pas de duplication | `EntiteAuditable`, `GrillePagineeViewModel`, styles et contrôles partagés, configurations EF | Relecture |

## Tests

```bash
dotnet test erpWpf.sln                                                          # tous les tests
dotnet test tests/erpWeb.UnitTests --filter "FullyQualifiedName~Architecture"   # règles d'architecture
```

Les tests unitaires utilisent SQLite en mémoire et des services simulés pour les ViewModels. Les tests d'intégration construisent la composition réelle de l'application et créent puis suppriment une base `erpWeb_Tests_<guid>` sur LocalDB, avec les migrations réelles ; pour un autre serveur (CI, conteneur), définir la variable `ERPWEB_TESTS_SQLSERVER` (chaîne de connexion sans nom de base).

## Gestion de version — GitHub Flow

Branches `<type>/<description>`, commits Conventional Commits, une Pull Request par évolution, squash merge.
Détails : [CONTRIBUTING.md](CONTRIBUTING.md). La CI (`.github/workflows/ci.yml`, job `build-test` sur `windows-latest` avec LocalDB) exécute build, vérification du format et tests sur chaque Pull Request.

### Protection de la branche main

À configurer sur GitHub une fois le dépôt distant créé (**Settings → Branches → Add rule** pour `main`, et **Settings → General → Pull Requests**) :

- Pull Request obligatoire avant fusion, **1 approbation** minimum, approbations invalidées par un nouveau commit
- Contrôle de statut `build-test` requis, branche à jour avant fusion
- Historique linéaire, force push et suppression interdits
- **Squash merge** uniquement, suppression automatique des branches fusionnées

Équivalent avec GitHub CLI :

```bash
gh api -X PUT repos/<proprietaire>/<depot>/branches/main/protection \
  -H "Accept: application/vnd.github+json" \
  --input - <<'JSON'
{
  "required_status_checks": { "strict": true, "contexts": ["build-test"] },
  "enforce_admins": true,
  "required_pull_request_reviews": { "required_approving_review_count": 1, "dismiss_stale_reviews": true },
  "restrictions": null,
  "required_linear_history": true,
  "allow_force_pushes": false,
  "allow_deletions": false
}
JSON

gh api -X PATCH repos/<proprietaire>/<depot> \
  -F allow_squash_merge=true -F allow_merge_commit=false -F allow_rebase_merge=false -F delete_branch_on_merge=true
```

## Outillage Claude Code

| Élément | Emplacement | Rôle |
| --- | --- | --- |
| Instructions | `CLAUDE.md` (racine, `src/*/`, `tests/`) | Commandes, architecture, règles de conception, MVVM et nommage |
| Permissions | `.claude/settings.json` | Commandes autorisées ; `git push --force` et `dotnet ef database drop` interdits |
| Hooks Git | `.claude/hooks/garde-git.ps1`, `valider-commit.ps1`, `garde-migrations.ps1` | Aucun commit sur `main`, Conventional Commits, migrations fusionnées protégées |
| Hooks qualité | `.claude/hooks/verifier-regles.ps1`, `formater.ps1`, `verifier-build.ps1` | Anti-patterns STUPID et règles MVVM signalés, formatage, build de `erpWpf.sln` et tests d'architecture en fin de tour |
| Agents | `.claude/agents/` | `revue-conception`, `redacteur-tests`, `expert-migrations` |
| Skills | `.claude/skills/` | `/nouveau-module`, `/nouveau-widget`, `/demarrer-branche`, `/preparer-pr` |

Les hooks nécessitent Windows PowerShell (`powershell.exe`) et un dépôt git.
