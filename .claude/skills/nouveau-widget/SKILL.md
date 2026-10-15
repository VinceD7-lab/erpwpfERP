---
name: nouveau-widget
description: Ajoute un widget au tableau de bord d'erpWpf (implémentation d'IWidgetTableauDeBord, rendu générique par type de données, permission, tests). À utiliser quand l'utilisateur demande un indicateur, un graphique, une liste ou des raccourcis sur le tableau de bord.
---

# Nouveau widget du tableau de bord

Les widgets sont découverts automatiquement (`DependencyInjection.AddCore`) et rendus selon le type de leurs données (`src/erpWpf.Wpf/Ressources/ModelesWidgets.xaml`) : **aucune modification** du tableau de bord n'est nécessaire (Open/Closed).

## 1. Cadrage

Obtenir si non fournis : titre, donnée affichée et sa source (service Core existant ou `IAppDbContext`), permission requise (ou aucune), position (`Ordre` : les widgets existants utilisent 10, 20, 30, 40).

Choisir le type de données existant le plus adapté :

| Type | Rendu WPF | Exemple |
| --- | --- | --- |
| `DonneesIndicateur(Valeur, Libelle, Detail)` | Chiffre clé | `WidgetUtilisateursActifs` |
| `DonneesGraphique(TypeGraphique, Etiquettes, Series)` | Graphique OxyPlot (`line` ou `bar`) via `DonneesGraphiqueVersModele` | `WidgetActiviteAudit` |
| `DonneesListe(Elements, MessageVide)` | Liste d'éléments, lien de navigation si `Destination` | `WidgetDerniersDocuments` |
| `DonneesRaccourcis(Raccourcis)` | Boutons de navigation filtrés par permission | `WidgetRaccourcisAdministration` |

## 2. Implémentation

1. Créer `src/erpWeb.Core/TableauDeBord/Widgets/Widget<Nom>.cs` : `sealed`, implémente `IWidgetTableauDeBord`.
   - `Nom` : kebab-case unique (identifie le widget au rafraîchissement).
   - `Icone` : nom neutre de `TableauDeBord/Icones` (ajouter une constante si besoin, et son glyphe Segoe Fluent Icons dans `NomIconeVersGlyphe` côté WPF).
   - `PermissionRequise` : constante de `Permissions`, ou `null`.
   - Liens : `Destination` = clé de `Communs/Destinations` (jamais d'URL) ; une action sans écran (ex. téléchargement) est une `DefinitionAction` enregistrée dans `EnregistrementModules`.
   - Dépendances : services Core ou `IAppDbContext`, jamais Infrastructure ; dates via `TimeProvider`.
   - Déléguer les requêtes non triviales à un service Core plutôt que de les écrire dans le widget.
2. **Seulement si aucun type existant ne convient** :
   - ajouter un `sealed record Donnees<Type>(...) : DonneesWidget` dans `TableauDeBord/DonneesWidget.cs` ;
   - ajouter son `DataTemplate DataType="{x:Type tdb:Donnees<Type>}"` dans `Ressources/ModelesWidgets.xaml` (commandes du widget via `RelativeSource AncestorType=vues:WidgetVue`) ;
   - logique de mise en forme non triviale : un convertisseur dans `Ressources/Convertisseurs/`, jamais de code-behind.

## 3. Tests

- `tests/erpWeb.UnitTests/Core/WidgetsTests.cs` : test du widget (données sources simulées, type et contenu du résultat) ; **mettre à jour le nombre attendu** dans `AddCore_EnregistreAutomatiquementTousLesWidgets`.
- `tests/erpWeb.UnitTests/Wpf/ViewModelsTests.cs` : si le widget dépend de permissions par élément, vérifier le filtrage dans `WidgetViewModel`.
- `tests/erpWeb.IntegrationTests/ParcoursApplicationTests.cs` : ajouter le `Nom` du widget à la liste de `TableauDeBord_Administrateur_AfficheLesQuatreWidgetsAvecLeurDonnees` (renommer le test si le nombre change).

## 4. Vérifications

`dotnet build erpWpf.sln`, `dotnet test erpWpf.sln`, vérification visuelle (`dotnet run --project src/erpWpf.Wpf`, bouton Rafraîchir), puis commit `feat(tableau-de-bord): widget <titre>`.
