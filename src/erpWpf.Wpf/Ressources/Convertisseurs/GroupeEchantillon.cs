using System.Globalization;
using System.Windows.Data;
using erpWeb.Core.Echantillons;
using erpWpf.Wpf.ViewModels.Echantillons;

namespace erpWpf.Wpf.Ressources.Convertisseurs;

/// <summary>Clé de regroupement des résultats d'échantillons (PropertyGroupDescription sans propriété).</summary>
public sealed class GroupeEchantillon : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ResultatEchantillonDto resultat ? ResultatsEchantillonsViewModel.Groupe(resultat) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
