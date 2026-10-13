using System.Globalization;
using System.Windows.Data;
using erpWeb.Core.TableauDeBord;
using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Legends;
using OxyPlot.Series;

namespace erpWpf.Wpf.Ressources.Convertisseurs;

/// <summary>
/// DonneesGraphique → modèle OxyPlot (ex-initialisation Chart.js de site.js) : axe des valeurs à partir de 0
/// en graduations entières, légende seulement s'il y a plusieurs séries. Types : « line » (défaut) et « bar ».
/// </summary>
public sealed class DonneesGraphiqueVersModele : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not DonneesGraphique donnees)
        {
            return null;
        }

        var modele = new PlotModel { PlotAreaBorderThickness = new OxyThickness(0, 0, 0, 1) };
        modele.Axes.Add(new CategoryAxis
        {
            Position = AxisPosition.Bottom,
            ItemsSource = donnees.Etiquettes,
            GapWidth = 0.3,
            Angle = donnees.Etiquettes.Count > 12 ? -45 : 0,
        });
        modele.Axes.Add(new LinearAxis
        {
            Position = AxisPosition.Left,
            Minimum = 0,
            MinimumMajorStep = 1,
            MinimumMinorStep = 1,
            MajorGridlineStyle = LineStyle.Dot,
            MajorGridlineColor = OxyColor.FromRgb(0xD9, 0xDE, 0xE5),
        });

        foreach (var serie in donnees.Series)
        {
            modele.Series.Add(donnees.TypeGraphique == "bar" ? CreerBarres(serie) : CreerCourbe(serie));
        }

        if (donnees.Series.Count > 1)
        {
            modele.Legends.Add(new Legend { LegendPosition = LegendPosition.TopRight });
        }

        return modele;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;

    private static LineSeries CreerCourbe(SerieGraphique serie)
    {
        var courbe = new LineSeries
        {
            Title = serie.Libelle,
            Color = OxyColor.FromRgb(0x1F, 0x5F, 0xBF),
            StrokeThickness = 2,
            MarkerType = MarkerType.Circle,
            MarkerSize = 2.5,
        };
        courbe.Points.AddRange(serie.Valeurs.Select((valeur, index) => new DataPoint(index, valeur)));
        return courbe;
    }

    private static BarSeries CreerBarres(SerieGraphique serie)
    {
        var barres = new BarSeries { Title = serie.Libelle, FillColor = OxyColor.FromRgb(0x1F, 0x5F, 0xBF) };
        barres.Items.AddRange(serie.Valeurs.Select(valeur => new BarItem(valeur)));
        return barres;
    }
}
