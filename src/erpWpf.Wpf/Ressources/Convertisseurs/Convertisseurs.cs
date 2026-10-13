using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using erpWeb.Core.Audit;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;

namespace erpWpf.Wpf.Ressources.Convertisseurs;

/// <summary>Vrai → Visible ; avec le paramètre « Inverse », faux → Visible.</summary>
public sealed class BooleenVersVisibilite : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var visible = value is true;
        if (parameter is "Inverse")
        {
            visible = !visible;
        }

        return visible ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is Visibility.Visible ^ parameter is "Inverse";
}

/// <summary>Valeur absente ou chaîne vide → Collapsed ; avec le paramètre « Inverse », l'inverse.</summary>
public sealed class PresenceVersVisibilite : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var present = value is not null && (value is not string texte || !string.IsNullOrWhiteSpace(texte));
        if (parameter is "Inverse")
        {
            present = !present;
        }

        return present ? Visibility.Visible : Visibility.Collapsed;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Nom d'icône neutre (erpWeb.Core.TableauDeBord.Icones, menu) → glyphe de la police Segoe Fluent Icons.</summary>
public sealed class NomIconeVersGlyphe : IValueConverter
{
    private static readonly Dictionary<string, string> _glyphes = new(StringComparer.OrdinalIgnoreCase)
    {
        ["tableau-de-bord"] = "\uE80F",
        ["dossier"] = "\uE8B7",
        ["clients"] = "\uE731",
        ["echantillons"] = "\uE9D9",
        ["factures"] = "\uE8C7",
        ["planning"] = "\uE787",
        ["utilisateurs"] = "\uE716",
        ["ajout-utilisateur"] = "\uE8FA",
        ["journal"] = "\uE81C",
        ["parametres"] = "\uE713",
        ["activite"] = "\uE9D2",
        ["document"] = "\uE8A5",
        ["depot"] = "\uE898",
        ["raccourcis"] = "\uE945",
        ["rafraichir"] = "\uE72C",
        ["menu"] = "\uE700",
        ["deconnexion"] = "\uF3B1",
        ["ajouter"] = "\uE710",
        ["modifier"] = "\uE70F",
        ["supprimer"] = "\uE74D",
        ["telecharger"] = "\uE896",
        ["imprimer"] = "\uE749",
        ["exporter"] = "\uE78C",
        ["precedent"] = "\uE76B",
        ["suivant"] = "\uE76C",
        ["premier"] = "\uE892",
        ["dernier"] = "\uE893",
        ["fermer"] = "\uE711",
        ["enregistrer"] = "\uE74E",
        ["annuler"] = "\uE7A7",
    };

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is string nom && _glyphes.TryGetValue(nom, out var glyphe) ? glyphe : "\uE897";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>
/// Libellé et couleur des badges de statut (factures, analyses d'échantillons, actions d'audit) :
/// reprend les formateurs JavaScript de la version web.
/// </summary>
public sealed class StatutVersBadge : IValueConverter
{
    private static readonly Dictionary<string, (string Libelle, string Couleur)> _statuts = new(StringComparer.Ordinal)
    {
        [StatutsFacture.Emise] = ("Émise", "#FF175CD3"),
        [StatutsFacture.Payee] = ("Payée", "#FF1E7B45"),
        [StatutsFacture.EnRetard] = ("En retard", "#FFB42318"),
        [StatutsAnalyse.Recu] = ("Reçu", "#FF667085"),
        [StatutsAnalyse.EnCours] = ("En cours", "#FF175CD3"),
        [StatutsAnalyse.Termine] = ("Terminé", "#FF1E7B45"),
        [nameof(ActionAudit.Creation)] = ("Création", "#FF1E7B45"),
        [nameof(ActionAudit.Modification)] = ("Modification", "#FF175CD3"),
        [nameof(ActionAudit.Suppression)] = ("Suppression", "#FFB42318"),
    };

    /// <summary>Paramètre « Couleur » : pinceau du badge ; sinon libellé.</summary>
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var cle = value?.ToString() ?? string.Empty;
        var (libelle, couleur) = _statuts.TryGetValue(cle, out var statut) ? statut : (cle, "#FF667085");
        return parameter is "Couleur"
            ? new SolidColorBrush((Color)ColorConverter.ConvertFromString(couleur))
            : libelle;
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Taille en octets → « 12,5 Ko ».</summary>
public sealed class TailleVersKilooctets : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is long octets ? string.Create(culture, $"{octets / 1024d:N1} Ko") : string.Empty;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}

/// <summary>Date UTC (base de données) → heure locale du poste.</summary>
public sealed class UtcVersLocal : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        var format = parameter as string ?? "dd/MM/yyyy HH:mm";
        return value switch
        {
            DateTime date => DateTime.SpecifyKind(date, DateTimeKind.Utc).ToLocalTime().ToString(format, culture),
            DateTimeOffset date => date.ToLocalTime().ToString(format, culture),
            _ => null,
        };
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => Binding.DoNothing;
}
