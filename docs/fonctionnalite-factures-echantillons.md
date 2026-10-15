# Branche `fonctionnalite/factures-echantillons`

> Plan réalisé sur la version web, puis porté sur WPF par la branche `refactorisation/migration-wpf` : les sections Core, Infrastructure et données sont inchangées ; la présentation, les tests d'intégration et la vérification décrits ci-dessous sont ceux de la version WPF.

## Plan : page « Factures » des échantillons + impression

### Contexte

Chaque échantillon doit avoir une facture. Aucune notion de facture ni de tarif n'existe encore. Choix validés : factures **calculées à la volée puis persistées** (table `Factures`, 1-1 avec `Echantillon`), impression via un **`FlowDocument` WPF** (aperçu à l'écran et `PrintDialog`, aucune dépendance ajoutée ; à l'origine page HTML imprimable).

Modèle à suivre : module Échantillons (lecture seule, grille paginée côté serveur `GrillePagineeViewModel`) — pas de CRUD, donc pas de `/nouveau-module` complet.

### Core (`src/erpWeb.Core/Factures/`)

- `Facture : EntiteAuditable` — `IdEchantillon` (FK unique, `Echantillon`), `NumeroFacture` (int, unique), `DateFacture` (DateOnly), `DateEcheance?`, `IdClient`, snapshot client figé : `NomClient`(100), `AdresseFacturation?`(255), `CodePostal?`(10), `Ville?`(80), `Pays?`(50), `ModePaiement?`(30), `MontantHorsTaxe`, `TauxTva`(5,2), `MontantTva`, `MontantToutesTaxesComprises` (12,2), `Remise?`(5,2), `Devise`(char 3), `StatutFacture`(20), `DatePaiement?`, `Commentaire?` (max). Noms français sans abréviation, mappés sur les colonnes demandées (CodeClient = `IdClient`).
- `StatutsFacture` (`Emise`, `Payee`, `EnRetard`), `LongueursFacture`.
- `OptionsFacturation` (section `Facturation`) : tarif HT par filière, `TauxTva` (20), `DelaiEcheanceEnJours` (30), `Devise` (EUR), `ModePaiementParDefaut`, remise client optionnelle.
- **Algorithme `CalculateurFacture`** (pur, testable, `TimeProvider`) : montant HT brut = tarif(filière) ; remise % → HT net = brut × (1 − remise/100) ; TVA = HT net × taux ; TTC = HT net + TVA ; arrondis `MidpointRounding.AwayFromZero` à 2 décimales, TTC = HT + TVA exact. Date facture = date de validation du résultat (sinon date de prélèvement/jour courant) ; échéance = date + délai ; statut `EnRetard` si échéance dépassée et non payée ; adresse/nom copiés du `Client`. Échantillon sans client : exclu (listé comme « non facturable »).
- `IGenerationFactures.GenererManquantesAsync(ct)` / `ServiceGenerationFactures` : crée en base les factures absentes (idempotent, `NumeroFacture` = max+1 séquentiel, transaction), appelé à l'ouverture de la liste.
- `IRechercheFactures.RechercherAsync(CriteresFactures, ct)` → `ResultatPagine<FactureDto>` (filtres : période, client, statut ; tri liste blanche), calqué sur `ServiceRechercheResultatsEchantillons`.
- `IServiceImpressionFacture.ObtenirAsync(id, ct)` → `FactureDto` détaillé (+ code-barres/filière de l'échantillon).
- Enregistrement manuel dans `DependencyInjection.cs`; `OptionsFacturation` liée dans `Hote/ConstructionHote` (`ValidateOnStart`).
- `Permissions.Factures.Lire` (+ ajout à `Toutes` et rôle `Utilisateur`).

### Infrastructure

- `IAppDbContext`/`AppDbContext` : `DbSet<Facture>`.
- `Donnees/Configurations/ConfigurationFacture.cs` (`ConfigurerChampsAudit()`, index uniques sur `NumeroFacture` et `IdEchantillon`, FK `Restrict` vers Client, relation 1-1 Échantillon, précisions decimal, CHECK montants ≥ 0).
- Migration `AjoutFactures` (via agent `expert-migrations`, une seule migration).
- `ServiceClients.SupprimerAsync` : refuser aussi si le client a des factures (déjà couvert par échantillons, vérifier).

### Présentation WPF (`src/erpWpf.Wpf/`)

- `ViewModels/Factures/ListeFacturesViewModel` (`GrillePagineeViewModel<FactureDto, ChampTriFactures>`) : filtres période, client, statut ; commande `Generer` protégée par `Permissions.Factures.Generer` ; commande `Imprimer` vers `Destinations.ImpressionFacture(id)`.
- `ViewModels/Factures/ImpressionFactureViewModel` (`INavigable`, paramètre = identifiant) : charge `ImpressionFactureDto`, retour à la liste avec message si la facture est introuvable, `Imprimer` via `IServiceImpression`.
- `Views/Factures/ListeFacturesVue.xaml` : `DataGrid` (tous les champs demandés, montants `N2`, badge de statut `ModeleBadge`), tri serveur `TriServeur`, `BarrePagination`, bouton icône d'impression par ligne.
- `Views/Factures/ImpressionFactureVue.xaml` : `FlowDocumentScrollViewer` alimenté par `FactureVersDocument` ; `Impression/ConstructeurDocumentFacture` construit le même document pour l'aperçu et l'impression (émetteur, client, ligne, remise, HT/TVA/TTC, échéance, mode de paiement, commentaire).
- `Hote/EnregistrementModules` : pages `Factures` et `Factures/Imprimer` (`Permissions.Factures.Lire`), entrée de menu (icône `factures`).

### Données de démonstration

Pas de générateur séparé nécessaire : la génération à la volée crée les factures des échantillons fictifs existants (statut payé/date paiement variés via valeurs déterministes en dev si besoin).

### Tests (agent `redacteur-tests`)

- Unitaires : `CalculateurFactureTests` (arrondis, remise, TVA, TTC, échéance, statut en retard), `ServiceGenerationFacturesTests` (idempotence, numérotation, échantillon sans client), `ServiceRechercheFacturesTests` (filtres, tri, pagination).
- ViewModels : génération idempotente, permission de génération, tri hors liste blanche ignoré.
- Intégration (`HoteApplication`) : `FacturesTests` (génération, recherche par client, aperçu et impression, facture inconnue, accès refusé sans permission) et `ContraintesFacturesTests` (unicité, FK Restrict, CHECK).
- Test d'architecture existant doit rester vert (Core sans dépendance).

### Vérification

`dotnet build erpWpf.sln` ; `dotnet test erpWpf.sln` ; `dotnet format erpWpf.sln --verify-no-changes` ; `dotnet run --project src/erpWpf.Wpf` en Development : ouvrir Factures, générer les factures manquantes, vérifier une ligne par échantillon facturable, ouvrir l'aperçu et imprimer ; puis `/preparer-pr` et agent `revue-conception`.

### Point à confirmer à l'implémentation

Les tarifs par filière sont des valeurs de configuration provisoires (`appsettings.json`, section `Facturation`) à remplacer par les vrais tarifs.
