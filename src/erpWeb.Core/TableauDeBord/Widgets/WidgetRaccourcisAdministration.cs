using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;

namespace erpWeb.Core.TableauDeBord.Widgets;

public sealed class WidgetRaccourcisAdministration : IWidgetTableauDeBord
{
    private static readonly DonneesRaccourcis _raccourcis = new(
    [
        new Raccourci("Nouvel utilisateur", Icones.AjoutUtilisateur, Destinations.CreationUtilisateur, Permissions.Utilisateurs.Gerer),
        new Raccourci("Journal d'audit", Icones.Journal, Destinations.JournalAudit, Permissions.JournalAudit.Lire),
        new Raccourci("Déposer un document", Icones.Depot, Destinations.DepotDocument, Permissions.Documents.Deposer),
        new Raccourci("Paramètres", Icones.Parametres, Destinations.Parametres, Permissions.Parametres.Lire),
    ]);

    public string Nom => "raccourcis";

    public string Titre => "Raccourcis";

    public string Icone => Icones.Raccourcis;

    public int Ordre => 40;

    public string? PermissionRequise => null;

    public Task<DonneesWidget> ObtenirDonneesAsync(CancellationToken jetonAnnulation = default)
        => Task.FromResult<DonneesWidget>(_raccourcis);
}
