using System.Globalization;
using System.Windows.Data;
using erpWeb.Core.Factures;
using erpWpf.Wpf.Impression;

namespace erpWpf.Wpf.Ressources.Convertisseurs;

/// <summary>Aperçu à l'écran : même document que celui envoyé à l'imprimante.</summary>
public sealed class FactureVersDocument : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is ImpressionFactureDto impression ? ConstructeurDocumentFacture.Construire(impression) : null;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
