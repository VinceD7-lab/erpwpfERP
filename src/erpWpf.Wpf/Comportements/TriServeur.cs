using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace erpWpf.Wpf.Comportements;

/// <summary>
/// Tri côté serveur d'un DataGrid : un clic sur un en-tête exécute la commande avec le SortMemberPath de la colonne
/// au lieu de trier la page affichée. Le ViewModel décide (liste blanche) ; l'indicateur de tri suit
/// les propriétés <c>Tri</c> / <c>TriDescendant</c> liées.
/// </summary>
public static class TriServeur
{
    public static readonly DependencyProperty CommandeProperty = DependencyProperty.RegisterAttached(
        "Commande", typeof(ICommand), typeof(TriServeur), new PropertyMetadata(null, SurCommandeModifiee));

    public static readonly DependencyProperty ColonneTrieeProperty = DependencyProperty.RegisterAttached(
        "ColonneTriee", typeof(object), typeof(TriServeur), new PropertyMetadata(null, SurTriModifie));

    public static readonly DependencyProperty DescendantProperty = DependencyProperty.RegisterAttached(
        "Descendant", typeof(bool), typeof(TriServeur), new PropertyMetadata(false, SurTriModifie));

    public static ICommand? GetCommande(DependencyObject element) => (ICommand?)element.GetValue(CommandeProperty);

    public static void SetCommande(DependencyObject element, ICommand? valeur) => element.SetValue(CommandeProperty, valeur);

    public static object? GetColonneTriee(DependencyObject element) => element.GetValue(ColonneTrieeProperty);

    public static void SetColonneTriee(DependencyObject element, object? valeur) => element.SetValue(ColonneTrieeProperty, valeur);

    public static bool GetDescendant(DependencyObject element) => (bool)element.GetValue(DescendantProperty);

    public static void SetDescendant(DependencyObject element, bool valeur) => element.SetValue(DescendantProperty, valeur);

    private static void SurCommandeModifiee(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not DataGrid grille)
        {
            return;
        }

        grille.Sorting -= SurTri;
        grille.Loaded -= SurChargement;
        if (e.NewValue is not null)
        {
            grille.Sorting += SurTri;
            grille.Loaded += SurChargement;
        }
    }

    private static void SurChargement(object sender, RoutedEventArgs e) => AfficherIndicateur((DataGrid)sender);

    private static void SurTri(object sender, DataGridSortingEventArgs e)
    {
        e.Handled = true;
        var grille = (DataGrid)sender;
        var commande = GetCommande(grille);
        var colonne = e.Column.SortMemberPath;
        if (commande?.CanExecute(colonne) == true)
        {
            commande.Execute(colonne);
        }
    }

    private static void SurTriModifie(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is DataGrid grille)
        {
            AfficherIndicateur(grille);
        }
    }

    private static void AfficherIndicateur(DataGrid grille)
    {
        var colonneTriee = GetColonneTriee(grille)?.ToString();
        foreach (var colonne in grille.Columns)
        {
            colonne.SortDirection = colonne.SortMemberPath == colonneTriee
                ? GetDescendant(grille) ? ListSortDirection.Descending : ListSortDirection.Ascending
                : null;
        }
    }
}
