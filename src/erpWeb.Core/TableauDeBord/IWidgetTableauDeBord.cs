namespace erpWeb.Core.TableauDeBord;

/// <summary>
/// Widget du tableau de bord. Toute implémentation de Core est enregistrée automatiquement :
/// ajouter un widget ne nécessite aucune modification de la page.
/// </summary>
public interface IWidgetTableauDeBord
{
    /// <summary>Identifiant technique unique en kebab-case.</summary>
    string Nom { get; }

    string Titre { get; }

    /// <summary>Nom d'icône neutre (voir <see cref="Icones"/>).</summary>
    string Icone { get; }

    int Ordre { get; }

    /// <summary>Permission nécessaire pour afficher le widget ; null si accessible à tout utilisateur connecté.</summary>
    string? PermissionRequise { get; }

    Task<DonneesWidget> ObtenirDonneesAsync(CancellationToken jetonAnnulation = default);
}
