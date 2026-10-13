# erpWeb.Infrastructure — implémentations techniques

Seul projet autorisé à utiliser EF Core SQL Server (`Microsoft.Data.SqlClient`), MailKit, ClosedXML et le système de fichiers côté métier.

## Données

- `Donnees/AppDbContext` implémente `IAppDbContext` ; exposer chaque nouveau `DbSet` via `Set<T>()`.
- Une classe `IEntityTypeConfiguration<T>` par entité dans `Donnees/Configurations/` : nom de table en français, longueurs maximales explicites, index utiles, `ConfigurerChampsAudit()` pour les entités `EntiteAuditable`.
- `IntercepteurAudit` trace toutes les entités : ajouter toute nouvelle propriété sensible (secret, hash) à `_proprietesExclues`. L'utilisateur tracé vient de la session du poste (`IUtilisateurCourant` implémenté par `erpWpf.Wpf/Securite/SessionUtilisateur`).
- `InitialisateurDonnees` est idempotent : il synchronise rôles et permissions à chaque démarrage de l'application.
- Chaîne de connexion `ConnectionStrings:ParDefaut` : connexion directe du poste client au serveur SQL (sécurité intégrée Windows de préférence, voir README).

## Migrations

```bash
dotnet ef migrations add <NomEnFrancais> -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf -o Donnees/Migrations
```

- Projet de démarrage : `src/erpWpf.Wpf`, via `Hote/FabriqueContexteConception` (lit `appsettings.json` + `appsettings.{DOTNET_ENVIRONMENT}.json`, Development par défaut).
- Une migration par Pull Request, nom en PascalCase français (`AjoutTableClients`).
- Relire `Up`/`Down` : signaler toute perte de données (`DropColumn`, `DropTable`, changement de type).
- SQL Server : clé d'index limitée à 900 octets (`nvarchar(450)` au maximum), index unique sur une colonne nullable à filtrer (`HasFilter`), réduction de longueur ou changement de type à risque sur des données existantes : tester la migration sur une copie de base réaliste.
- Base locale : LocalDB (`(localdb)\MSSQLLocalDB`, base `erpWeb`) ; `dotnet ef database update -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf`.
- Production : pas de migration au démarrage d'un poste ; un administrateur applique un bundle (`dotnet ef migrations bundle -p src/erpWeb.Infrastructure -s src/erpWpf.Wpf -r win-x64 --self-contained`, puis `efbundle.exe --connection "…"`), produit par `publier-application.ps1`.
- Ne jamais modifier une migration présente sur `main` (hook `garde-migrations`) ; en cas de conflit de snapshot, utiliser l'agent `expert-migrations`.

## Services

- Enregistrer les implémentations dans `DependencyInjection.AddInfrastructure` (Scoped par défaut, Singleton seulement si sans état).
- Options typées : `services.Configure<T>(configuration.GetSection(T.Section))`.
- `Stockage:Dossier` relatif : résolu sous le répertoire de données du poste (`%LOCALAPPDATA%\erpWpf`) ; absolu ou UNC en production multi-postes.
- Identity : `AddStockageIdentite()` (stores EF, messages en français) et `AddJetonsIdentite()` (jetons TOTP, sans Data Protection).
