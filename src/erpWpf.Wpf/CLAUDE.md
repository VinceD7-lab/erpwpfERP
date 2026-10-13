# erpWpf.Wpf — présentation WPF (MVVM)

`App.xaml.cs` et `Hote/` sont la composition root : seuls endroits autorisés à référencer Infrastructure et `IServiceProvider` (vérifié par `ArchitectureTests` et le hook `verifier-regles`).

## Organisation

| Dossier | Rôle |
|---|---|
| `Hote/` | `ConstructionHote` (hôte générique, configuration, DI — partagé avec les tests d'intégration), `DependencyInjection.AddPresentation`, `EnregistrementModules` (pages, actions, menu par module), `ExecuteurOperations`, `FabriqueViewModels`, `GestionnaireFenetres`, `FabriqueContexteConception` (dotnet ef) |
| `Securite/` | `SessionUtilisateur` (session du poste, fournit `IUtilisateurCourant` à l'audit), `IAutorisation`, `SurveillanceSession` (relecture toutes les minutes : compte désactivé ou mot de passe réinitialisé → déconnexion) |
| `Navigation/` | `IServiceNavigation` (destination `Module/Action/parametre`, voir `erpWeb.Core.Communs.Destinations`), `DefinitionPage`, `DefinitionAction`, `EntreeMenu` |
| `Services/` | Services d'interface derrière une interface : `IExecuteurOperations`, `IServiceDialogues`, `IServiceFichiers`, `IServiceImpression`, `IServiceNotifications`, `IPreferencesLocales` |
| `ViewModels/<Module>/` | Un ViewModel par écran ; `Communs/` : `ViewModelBase`, `INavigable`, `IConfirmationDepart`, `GrillePagineeViewModel`, `OptionFiltre`, `LibellesStatuts` |
| `Views/<Module>/` | `UserControl` XAML, code-behind limité à `InitializeComponent()` |
| `Fenetres/` | `FenetreAccueil` (connexion, inscription), `FenetrePrincipale` (menu, page, bandeau), `FenetreErreur` |
| `Controles/` | `EnTetePage`, `BarrePagination`, `ChampsClient`, `ProxyLiaison` |
| `Comportements/` | Propriétés attachées : `LiaisonMotDePasse`, `TriServeur`, `ColonnesPlanning` |
| `Ressources/` | `Couleurs.xaml`, `Styles.xaml`, `ModelesPages.xaml` (ViewModel → vue), `ModelesWidgets.xaml` (type de données → rendu), `Convertisseurs/` |
| `Impression/` | `ConstructeurDocumentFacture` (`FlowDocument` partagé par l'aperçu et l'impression) |

## ViewModels

- Héritent de `ViewModelBase` (écrans) ou `ObservableObject` (éléments) ; `sealed partial`, transients, résolus par DI.
- **Ne reçoivent que des singletons** ; les services de Core (scoped) passent par `IExecuteurOperations.ExecuterAsync<IService, TResultat>(…)` — une portée et un `DbContext` par opération. `ValidateScopes` en développement détecte toute capture.
- Propriétés : `[ObservableProperty] public partial string Nom { get; set; }` (champs `[ObservableProperty]` interdits : avertissement MVVMTK0045 = erreur).
- Commandes : `[RelayCommand]` sur des méthodes `Task …Async()` ; `ExecuterOccupeAsync(jeton => …)` pour l'indicateur d'occupation et le jeton annulé à la fermeture de l'écran (`JetonEcran`).
- Erreurs métier : `AfficherErreurs(resultat.Erreurs)` (résumé `ResumeErreurs` dans la vue) ; succès : `IServiceNotifications.Succes(…)` puis navigation.
- Écran chargé par la navigation : implémenter `INavigable.InitialiserAsync(parametre, jeton)` ; modifications non enregistrées : `IConfirmationDepart`.
- Grilles paginées et triées côté serveur : hériter de `GrillePagineeViewModel<TDto, TChampTri>` (le tri n'accepte que les valeurs de l'énumération `TChampTri`, liste blanche).
- Interdits dans `ViewModels/` (tests d'architecture + hook) : `IAppDbContext`, EF Core, `System.Windows.Controls/Media/Documents`, `MessageBox`, `Microsoft.Win32`, `IServiceProvider`. `System.Windows.Input` (`ICommand`) est autorisé.

## Permissions

- Chaque page déclare sa permission à l'enregistrement : `services.AjouterPage<ListeXxxViewModel>(Destinations.Xxx, Permissions.Xxx.Lire)` — la navigation affiche `AccesRefuseViewModel` sans elle (ex-`[Authorize]`).
- Les actions d'écriture vérifient la permission dans le ViewModel (`PeutGerer = autorisation.APermission(Permissions.Xxx.Gerer)` et `[RelayCommand(CanExecute = nameof(PeutGerer))]`) ; la vue masque les boutons avec `BooleenVersVisibilite`. **Jamais seulement masquer.**
- Menu : `services.AjouterMenu(new EntreeMenu(ordre, libellé, icône, destination, permission, section))` dans `EnregistrementModules`.
- Rappel : avec une connexion SQL directe, la frontière de sécurité réelle est le compte SQL (droits limités, voir README).

## Vues XAML

- Une vue par ViewModel, déclarée dans `Ressources/ModelesPages.xaml` (`DataTemplate DataType=…`) ; `d:DataContext="{d:DesignInstance …}"` pour l'IntelliSense.
- Styles et couleurs dans `Ressources/` uniquement (`StaticResource`) ; icônes par nom neutre via `NomIconeVersGlyphe` (police Segoe Fluent Icons).
- En-tête : `<controles:EnTetePage Titre="{Binding Titre}">boutons</controles:EnTetePage>`.
- `DataGrid` : colonnes explicites ; colonnes hors arbre visuel liées via `ProxyLiaison` (`{Binding Donnees.X, Source={StaticResource Ecran}}`) ; tri serveur via `comportements:TriServeur.Commande`, `SortMemberPath` = nom de la valeur de `TChampTri`.
- Mot de passe : `PasswordBox` + `comportements:LiaisonMotDePasse.MotDePasse`.
- Accessibilité : `AutomationProperties.Name` sur les boutons icônes et champs sans libellé visible, `IsDefault`/`IsCancel` sur les formulaires, ordre de tabulation naturel.
- Pas de `MessageBox`, d'`OpenFileDialog` ni de `PrintDialog` dans une vue ou un ViewModel : passer par `IServiceDialogues`, `IServiceFichiers`, `IServiceImpression`.

## Tableau de bord

Rendu générique par type de données (`DonneesIndicateur`, `DonneesGraphique` via OxyPlot, `DonneesListe`, `DonneesRaccourcis`) dans `Ressources/ModelesWidgets.xaml` : un widget de Core qui réutilise un type existant n'ajoute aucune vue. Destinations et noms d'icônes neutres (`Destinations`, `Icones` dans Core).

## Démarrage

`App.xaml.cs` : culture fr-FR, hôte (`ConstructionHote`), `InitialiserDonneesAsync` (migrations si `BaseDeDonnees:AppliquerMigrationsAuDemarrage`), données de démonstration en Development, puis `GestionnaireFenetres`. Environnement : `DOTNET_ENVIRONMENT` (Development via `Properties/launchSettings.json` pour `dotnet run`, Production par défaut).
