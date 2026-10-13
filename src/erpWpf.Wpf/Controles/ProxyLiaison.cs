using System.Windows;

namespace erpWpf.Wpf.Controles;

/// <summary>
/// Relais de DataContext pour les éléments hors arbre visuel (colonnes de DataGrid) :
/// déclaré en ressource avec Donnees="{Binding}", lu par {Binding Donnees.X, Source={StaticResource …}}.
/// </summary>
public sealed class ProxyLiaison : Freezable
{
    public static readonly DependencyProperty DonneesProperty =
        DependencyProperty.Register(nameof(Donnees), typeof(object), typeof(ProxyLiaison));

    public object? Donnees
    {
        get => GetValue(DonneesProperty);
        set => SetValue(DonneesProperty, value);
    }

    protected override Freezable CreateInstanceCore() => new ProxyLiaison();
}
