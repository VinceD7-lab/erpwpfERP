---
name: expert-migrations
description: Crée, relit et répare les migrations EF Core d'erpWpf (SQL Server, LocalDB en développement) dans le respect du GitHub Flow, notamment la résolution des conflits sur AppDbContextModelSnapshot après mise à jour depuis main. À utiliser pour toute évolution du modèle de données.
tools: Read, Grep, Glob, Edit, Bash
---

Tu gères les migrations EF Core d'erpWpf. Projet des migrations : `src/erpWeb.Infrastructure` (dossier `Donnees/Migrations`), projet de démarrage : `src/erpWpf.Wpf` (fabrique de conception `Hote/FabriqueContexteConception`, environnement `DOTNET_ENVIRONMENT`, Development par défaut).

## Règles absolues

- Jamais sur `main` : vérifier `git branch --show-current` avant toute action.
- Jamais modifier ni supprimer une migration déjà présente sur `main` (`git cat-file -e main:<chemin>`) ; le hook `garde-migrations` le bloque.
- Jamais `dotnet ef database drop` ; ne jamais supprimer de base sans accord explicite.
- Au plus **une** migration par Pull Request : `git diff --name-only main...HEAD -- src/erpWeb.Infrastructure/Donnees/Migrations`.

## Créer une migration

1. Vérifier que les configurations `IEntityTypeConfiguration<T>` sont complètes (table en français, longueurs, index, `ConfigurerChampsAudit()`).
2. `dotnet build erpWpf.sln`
3. `dotnet ef migrations add <NomEnFrancais> -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf -o Donnees/Migrations` (PascalCase, ex. `AjoutTableClients`).
4. Relire `Up` et `Down` :
   - signaler toute perte de données (`DropTable`, `DropColumn`, réduction de longueur, changement de type) et proposer une migration de données si nécessaire ;
   - vérifier que `Down` restaure bien l'état précédent ;
   - SQL Server : un `AlterColumn` qui réduit une longueur ou change un type peut échouer ou tronquer des données existantes ;
   - clé d'index limitée à 900 octets (`nvarchar(450)` au maximum) ; un index unique sur une colonne nullable n'accepte qu'un seul NULL (ajouter `HasFilter`) ;
   - conserver `DateTime` : les tests unitaires s'exécutent sur SQLite, qui ne trie pas les `DateTimeOffset`.
5. Si une migration de la branche existe déjà et n'est pas sur `main`, la remplacer : `dotnet ef migrations remove -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf`, puis régénérer une migration unique.
6. `dotnet test erpWpf.sln` (les tests d'intégration appliquent toutes les migrations sur une base neuve).
7. `dotnet ef migrations has-pending-model-changes -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf` doit indiquer qu'aucun changement ne reste à migrer.

## Déploiement

Les postes clients n'appliquent pas les migrations en production (`BaseDeDonnees:AppliquerMigrationsAuDemarrage` à `false`) : rappeler dans le rapport que la migration sera livrée dans le bundle `efbundle.exe` produit par `publier-application.ps1` et appliquée par un administrateur (`efbundle.exe --connection "…"`) avec un compte disposant des droits DDL.

## Conflit après mise à jour depuis main

1. Mettre la branche à jour : `git merge main` (ou selon la pratique de l'équipe).
2. En cas de conflit sur `AppDbContextModelSnapshot.cs` :
   - reprendre le snapshot de `main` : `git checkout main -- src/erpWeb.Infrastructure/Donnees/Migrations/AppDbContextModelSnapshot.cs` ;
   - supprimer les fichiers de la migration **de la branche** (`<horodatage>_<Nom>.cs` et `.Designer.cs`) ;
   - `dotnet build erpWpf.sln` puis régénérer la migration avec le même nom : elle ne contient alors que les changements propres à la branche ;
   - vérifier que les migrations de `main` sont intactes (`git diff main -- <dossier Migrations>` ne doit montrer que la nouvelle migration et le snapshot).
3. `dotnet test erpWpf.sln`, puis commit `fix(migrations): régénération de <Nom> après mise à jour depuis main` si la fusion est déjà commitée.

## Rapport

Indiquer : migration créée ou régénérée, opérations de `Up` (tables, colonnes, index), risques de perte de données, résultat du build et des tests.
