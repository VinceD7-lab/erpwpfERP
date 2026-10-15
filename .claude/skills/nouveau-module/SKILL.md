---
name: nouveau-module
description: Crée un module métier complet dans erpWpf (entité, service, validateurs, mapping, permissions, ViewModels, vues XAML, navigation, menu, tests, migration) en suivant l'architecture Core / Infrastructure / Wpf. À utiliser quand l'utilisateur demande un nouveau module, une nouvelle entité gérée ou un nouvel écran CRUD (ex. clients, fournisseurs, articles).
---

# Nouveau module métier

Modules de référence à imiter : **Documents** (`src/erpWeb.Core/Documents`, `src/erpWpf.Wpf/ViewModels/Documents`, `Views/Documents`) pour un CRUD simple, **Clients** pour un formulaire partagé (`ChampsClientViewModel`, `Controles/ChampsClient.xaml`) et l'édition en ligne.

## 0. Cadrage

1. Vérifier la branche (`git branch --show-current`) : si `main`, appliquer d'abord le skill `demarrer-branche` (`fonctionnalite/<module>`).
2. Obtenir, si non fournis : nom du module (pluriel, français, ex. `Clients`), entité (singulier, ex. `Client`), champs avec types, obligatoires et longueurs, actions nécessaires (lecture, création, modification, suppression), rôles concernés, volume attendu (liste simple ou grille paginée côté serveur).
3. Proposer ce découpage à l'utilisateur et attendre sa validation avant de générer.

## 1. Core — `src/erpWeb.Core/<Module>/`

- `<Entite>.cs` : hérite de `EntiteAuditable`, propriétés `string` initialisées à `string.Empty`.
- `Dtos.cs` : `<Entite>Dto` (propriétés `init`), `Creation<Entite>Dto` et `Modification<Entite>Dto` (propriétés `set`).
- `Validateurs.cs` : un `AbstractValidator<T>` par DTO d'écriture, `WithName` en français, longueurs identiques à la configuration EF (constantes `Longueurs<Entite>` partagées avec la vue).
- `IService<Module>.cs` / `Service<Module>.cs` : `IAppDbContext`, validateurs, `IMapper` ; retours `ResultatOperation` / `ResultatOperation<int>` ; `CancellationToken jetonAnnulation`.
- Grille volumineuse : `Criteres<Module>` (pagination, tri par énumération `ChampTri<Module>`, liste blanche) et `IRecherche<Module>` retournant `ResultatPagine<T>`.
- `ConfigurationMapping<Module>.cs` : `IRegister`.
- `Communs/IAppDbContext.cs` : ajouter `DbSet<<Entite>> <Module> { get; }`.
- `Communs/Destinations.cs` : `public const string <Module> = "<Module>";`, création, route paramétrée de modification et sa fonction (`Modification<Entite>(int id)`).
- `Autorisation/Permissions.cs` : classe imbriquée `<Module>` (`Lire`, `Gerer`…), ajout dans `Toutes` ; `RolesApplication.PermissionsParRole` si le rôle `Utilisateur` est concerné.
- `DependencyInjection.AddCore` : `services.AddScoped<IService<Module>, Service<Module>>();`

## 2. Infrastructure — `src/erpWeb.Infrastructure/`

- `Donnees/Configurations/Configuration<Entite>.cs` : `ToTable("<Module>")`, `ConfigurerChampsAudit()`, `HasMaxLength`, `IsRequired`, index.
- `Donnees/AppDbContext.cs` : `public DbSet<<Entite>> <Module> => Set<<Entite>>();`

## 3. Présentation WPF — `src/erpWpf.Wpf/`

- `ViewModels/<Module>/Liste<Module>ViewModel.cs` : `sealed partial`, hérite de `ViewModelBase` et implémente `INavigable` (ou hérite de `GrillePagineeViewModel<<Entite>Dto, ChampTri<Module>>` pour une grille serveur) ; services de Core **uniquement** via `IExecuteurOperations` ; `PeutGerer = autorisation.APermission(Permissions.<Module>.Gerer)` ; commandes `Creer`, `Modifier`, `Supprimer` en `[RelayCommand(CanExecute = nameof(PeutGerer))]`, suppression confirmée par `IServiceDialogues`, messages par `IServiceNotifications`.
- `ViewModels/<Module>/Edition<Entite>ViewModel.cs` : `INavigable` (paramètre = identifiant, absent en création), propriétés partielles `[ObservableProperty]`, `EnregistrerCommand` → `AfficherErreurs(resultat.Erreurs)` en échec, sinon notification et `NaviguerAsync(Destinations.<Module>)`.
- `Views/<Module>/Liste<Module>Vue.xaml` : `EnTetePage` avec bouton masqué sans permission (`BooleenVersVisibilite`), `DataGrid` en colonnes explicites, colonne Actions liée par `ProxyLiaison`, `AutomationProperties.Name` sur les boutons icônes ; grille serveur : `comportements:TriServeur.*` et `controles:BarrePagination`.
- `Views/<Module>/Edition<Entite>Vue.xaml` : `ItemsControl` `ResumeErreurs`, champs avec `MaxLength="{x:Static …Longueurs<Entite>.Champ}"`, boutons `IsDefault`/`IsCancel` ; factoriser les champs partagés dans un contrôle (`Controles/Champs<Entite>.xaml`) et un ViewModel de champs.
- Code-behind : `InitializeComponent()` uniquement.
- `Ressources/ModelesPages.xaml` : un `DataTemplate DataType` par ViewModel d'écran.
- `Hote/EnregistrementModules.cs` : section du module avec `AjouterPage<…>(Destinations.<Module>, Permissions.<Module>.Lire)`, pages de création et modification avec la permission d'écriture, `AjouterMenu(new EntreeMenu(ordre, "<Libellé>", "<icone>", Destinations.<Module>, Permissions.<Module>.Lire, section))` ; ajouter le glyphe de l'icône dans `Ressources/Convertisseurs/Convertisseurs.cs` (`NomIconeVersGlyphe`) si nouveau.

## 4. Tests

- `tests/erpWeb.UnitTests/Core/Service<Module>Tests.cs` : `BaseDonneesTest`, cas nominal, validation en échec, introuvable, audit renseigné.
- `tests/erpWeb.UnitTests/Wpf/` : ViewModels avec `ExecuteurOperationsTest` et services simulés (chargement, erreurs affichées, commande non exécutable sans permission, confirmation de suppression).
- `tests/erpWeb.IntegrationTests/` : l'administrateur ouvre `Destinations.<Module>` (ViewModel attendu), un compte sans rôle obtient `AccesRefuseViewModel`, création de bout en bout via le ViewModel d'édition (`HoteApplication`).
- `Composition_ChaqueEcranEtChaqueAction_EstResolvable` couvre automatiquement les nouvelles pages.
- Déléguer à l'agent `redacteur-tests` si le volume est important.

## 5. Migration et vérifications

1. Migration via l'agent `expert-migrations` : `Ajout<Module>`.
2. `dotnet build erpWpf.sln`, `dotnet format erpWpf.sln --verify-no-changes`, `dotnet test erpWpf.sln`.
3. Lancer l'application (`dotnet run --project src/erpWpf.Wpf`) et parcourir les écrans du module.
4. Lancer l'agent `revue-conception` et corriger les constats bloquants.
5. Commits Conventional Commits, par exemple `feat(<module>): entité, service et validateurs`, `feat(<module>): écrans WPF et permissions`, `feat(infrastructure): migration Ajout<Module>`, `test(<module>): tests du service et des ViewModels`.
