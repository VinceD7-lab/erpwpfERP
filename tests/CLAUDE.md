# Tests

```bash
dotnet test erpWpf.sln
dotnet test tests/erpWeb.UnitTests --filter "FullyQualifiedName~Architecture"
```

Les deux projets ciblent `net10.0-windows` (`UseWPF`) pour référencer `erpWpf.Wpf` ; `System.IO` est ajouté en using global (retiré par `UseWPF`).

## Conventions

- Nommage `Methode_Scenario_ResultatAttendu` ; blocs Arrange / Act / Assert séparés par une ligne vide.
- Au moins un test par service, par widget et par ViewModel d'écran ; couvrir les cas d'échec (validation, élément introuvable, permission absente).

## Tests unitaires (`erpWeb.UnitTests`)

- `Outils/BaseDonneesTest` : SQLite en mémoire + `IntercepteurAudit` + `FakeTimeProvider` (`DateReference`) + utilisateur courant simulé ; `BaseDonneesTest.CreerMapper()` pour Mapster. Ne pas mocker les `DbSet`.
- `Outils/HoteIdentiteTest` : Identity réel (UserManager/RoleManager, verrouillage, jetons TOTP) sur SQLite — pas de mock de `UserManager` (modèles : `Core/ServiceUtilisateursTests`, `Core/ServiceAuthentificationTests`).
- `Outils/ExecuteurOperationsTest` : exécuteur des tests de ViewModels, qui fournit les services (mocks Moq) par type (modèle : `Wpf/ViewModelsTests`).
- Moq pour les interfaces externes (`IStockageFichiers`, `IServiceEmail`, `IRechargementConfiguration`) et d'interface (`IServiceDialogues`, `IServiceNotifications`, `IAutorisation`).
- Bogus `new Faker("fr")` pour des données réalistes ; emails générés sur `@erpweb.local`.
- Minuteries (`SurveillanceSession`, `ServiceNotifications`) : `FakeTimeProvider.Advance`.
- `Architecture/` : ajouter une règle NetArchTest dès qu'une nouvelle contrainte de dépendance apparaît (couches, MVVM, ViewModels sans types graphiques).
- Dans `namespace erpWeb.UnitTests.Core`, qualifier `global::erpWeb.Core` en cas d'ambiguïté.

## Tests d'intégration (`erpWeb.IntegrationTests`)

- `IClassFixture<HoteApplication>` : composition réelle (`ConstructionHote.AjouterServices`, `ValidateScopes` + `ValidateOnBuild`), environnement `Test`, base SQL Server temporaire `erpWeb_Tests_<guid>` migrée puis supprimée (LocalDB, ou serveur défini par la variable `ERPWEB_TESTS_SQLSERVER`), administrateur `HoteApplication.EmailAdministrateur`.
- Session : `ConnecterAdministrateurAsync()`, `ConnecterUtilisateurStandardAsync()`, `ConnecterNouveauCompteAsync()` (sans rôle, donc sans permission).
- Parcours : naviguer avec `IServiceNavigation` puis piloter le ViewModel de `PageCourante` par ses commandes ; vérifier en base avec `CreerContexte(out portee)`.
- Doubles d'interface (`DoublesInterface.cs`) : `DialoguesTest` (réponse configurable), `FichiersTest` (dossier temporaire), `ImpressionTest`.
- Contraintes du schéma SQL Server (index uniques, CHECK, clés étrangères) : `Contraintes*Tests`.
