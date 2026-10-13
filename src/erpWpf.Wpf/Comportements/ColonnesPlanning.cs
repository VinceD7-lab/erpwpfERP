using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using erpWpf.Wpf.ViewModels.PlanningAnalyses;

namespace erpWpf.Wpf.Comportements;

/// <summary>
/// Génère une colonne par jour du mois dans la grille du planning (le nombre de jours varie d'un mois à l'autre).
/// Les colonnes déclarées en XAML (nom du client) sont conservées ; les week-ends sont grisés.
/// </summary>
public static class ColonnesPlanning
{
    public static readonly DependencyProperty JoursProperty = DependencyProperty.RegisterAttached(
        "Jours", typeof(IReadOnlyList<JourPlanningViewModel>), typeof(ColonnesPlanning), new PropertyMetadata(null, SurJoursModifies));

    private static readonly DependencyProperty NombreColonnesFixesProperty =
        DependencyProperty.RegisterAttached("NombreColonnesFixes", typeof(int), typeof(ColonnesPlanning), new PropertyMetadata(-1));

    public static IReadOnlyList<JourPlanningViewModel>? GetJours(DependencyObject element) => (IReadOnlyList<JourPlanningViewModel>?)element.GetValue(JoursProperty);

    public static void SetJours(DependencyObject element, IReadOnlyList<JourPlanningViewModel>? valeur) => element.SetValue(JoursProperty, valeur);

    private static void SurJoursModifies(DependencyObject element, DependencyPropertyChangedEventArgs e)
    {
        if (element is not DataGrid grille)
        {
            return;
        }

        var fixes = (int)grille.GetValue(NombreColonnesFixesProperty);
        if (fixes < 0)
        {
            fixes = grille.Columns.Count;
            grille.SetValue(NombreColonnesFixesProperty, fixes);
        }

        while (grille.Columns.Count > fixes)
        {
            grille.Columns.RemoveAt(grille.Columns.Count - 1);
        }

        var jours = e.NewValue as IReadOnlyList<JourPlanningViewModel> ?? [];
        for (var index = 0; index < jours.Count; index++)
        {
            grille.Columns.Add(CreerColonne(grille, jours[index], index));
        }
    }

    private static DataGridTemplateColumn CreerColonne(DataGrid grille, JourPlanningViewModel jour, int index)
    {
        var liste = new FrameworkElementFactory(typeof(ComboBox));
        liste.SetValue(ItemsControl.DisplayMemberPathProperty, "Libelle");
        liste.SetValue(Selector.SelectedValuePathProperty, "Valeur");
        liste.SetValue(FrameworkElement.MinWidthProperty, 44d);
        liste.SetValue(Control.PaddingProperty, new Thickness(4, 2, 2, 2));
        liste.SetBinding(ItemsControl.ItemsSourceProperty, new Binding("DataContext.TypesAnalyse")
        {
            RelativeSource = new RelativeSource(RelativeSourceMode.FindAncestor, typeof(DataGrid), 1),
        });
        liste.SetBinding(Selector.SelectedValueProperty, new Binding($"Cases[{index}].TypeAnalyse")
        {
            Mode = BindingMode.TwoWay,
            UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
        });
        liste.SetBinding(UIElement.IsEnabledProperty, new Binding($"Cases[{index}].EstModifiable"));
        liste.SetValue(UIElement.VisibilityProperty, jour.EstOuvre ? Visibility.Visible : Visibility.Collapsed);
        liste.SetValue(System.Windows.Automation.AutomationProperties.NameProperty, $"Analyse du {jour.Date:dd/MM}");

        var colonne = new DataGridTemplateColumn
        {
            Header = new TextBlock { Text = jour.Entete, TextAlignment = TextAlignment.Center },
            CellTemplate = new DataTemplate { VisualTree = liste },
            Width = new DataGridLength(52),
            CanUserSort = false,
            CanUserResize = false,
        };

        if (!jour.EstOuvre)
        {
            var style = new Style(typeof(DataGridCell), grille.TryFindResource(typeof(DataGridCell)) as Style);
            style.Setters.Add(new Setter(Control.BackgroundProperty, new SolidColorBrush(Color.FromRgb(0xE9, 0xEC, 0xF0))));
            colonne.CellStyle = style;
        }

        return colonne;
    }
}
