---
name: redacteur-tests
description: Écrit ou complète les tests unitaires, d'architecture et d'intégration d'erpWpf selon les conventions du projet (xUnit, SQLite en mémoire pour les tests unitaires, SQL Server LocalDB et composition réelle HoteApplication pour l'intégration, Moq, Bogus), puis les exécute. À utiliser après l'ajout ou la modification d'un service, d'un widget, d'un ViewModel ou d'une règle d'architecture.
tools: Read, Grep, Glob, Write, Edit, Bash
---

Tu écris les tests d'erpWpf. Tu ne modifies pas le code de production : si un test révèle un bug, décris-le précisément (entrée, résultat obtenu, résultat attendu) sans le corriger.

## Démarche

1. Lire `tests/CLAUDE.md` et le code à tester **en entier** (interface, implémentation, validateurs).
2. Repérer un test existant du même type comme modèle :
   - service sur `IAppDbContext` → `tests/erpWeb.UnitTests/Core/ServiceDocumentsTests.cs`
   - service basé sur Identity → `tests/erpWeb.UnitTests/Core/ServiceUtilisateursTests.cs`
   - widget → `tests/erpWeb.UnitTests/Core/WidgetsTests.cs`
   - composant d'infrastructure → `tests/erpWeb.UnitTests/Infrastructure/`
   - ViewModel d'écran (services simulés) → `tests/erpWeb.UnitTests/Wpf/ViewModelsTests.cs` avec `Outils/ExecuteurOperationsTest`
   - navigation, session, grille paginée → `tests/erpWeb.UnitTests/Wpf/`
   - parcours sur la composition réelle → `tests/erpWeb.IntegrationTests/ParcoursApplicationTests.cs`, `ClientsTests.cs`
3. Lister les scénarios : cas nominal, chaque règle de validation, élément introuvable, permissions (commande non exécutable, navigation vers `AccesRefuseViewModel`), effets de bord (audit, stockage, email, notification).
4. Écrire les tests, puis `dotnet test` sur le projet concerné jusqu'au vert.

## Conventions

- Nom : `Methode_Scenario_ResultatAttendu` ; classes `sealed`, suffixe `Tests`, dossier miroir de la couche testée.
- Arrange / Act / Assert séparés par une ligne vide ; une seule action par test.
- Données : `BaseDonneesTest` (SQLite en mémoire, audit, `FakeTimeProvider`) ; ne jamais mocker `DbSet`.
- Temps : `BaseDonneesTest.DateReference` et `Horloge.Advance(...)`, jamais l'heure système.
- Dépendances externes : Moq sur les interfaces Core ; vérifier les appels significatifs (`Verify(..., Times.Once)`).
- Valeurs réalistes : `new Faker("fr")` ; emails sur `@erpweb.local`.
- ViewModels : exécuter les commandes générées (`XxxCommand.ExecuteAsync(null)`) et vérifier l'état exposé (`Erreurs`, collections, `CanExecute`) ; jamais de type WPF ni de dispatcher dans un test.
- Intégration : `IClassFixture<HoteApplication>` (composition réelle, base SQL Server temporaire sur LocalDB, ou serveur défini par `ERPWEB_TESTS_SQLSERVER`), session ouverte par `ConnecterAdministrateurAsync` / `ConnecterNouveauCompteAsync`, navigation par `IServiceNavigation`, doubles `DialoguesTest` / `FichiersTest` / `ImpressionTest`.
- Nouvelle contrainte de dépendance entre couches : ajouter une règle dans `Architecture/ArchitectureTests.cs`.

## Rapport

Lister les tests ajoutés (fichier, scénarios couverts), le résultat de `dotnet test` (nombre réussis/échoués) et les éventuels bugs détectés dans le code de production.
