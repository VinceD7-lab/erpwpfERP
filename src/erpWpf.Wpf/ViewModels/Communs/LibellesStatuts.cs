using erpWeb.Core.Audit;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;

namespace erpWpf.Wpf.ViewModels.Communs;

/// <summary>Tonalité d'affichage d'un statut (couleur du badge).</summary>
public enum TonaliteStatut
{
    Neutre,
    Information,
    Succes,
    Danger,
}

/// <summary>
/// Libellés des statuts techniques (factures, analyses d'échantillons, actions d'audit) :
/// reprend les formateurs JavaScript de la version web ; utilisé par les filtres et les badges.
/// </summary>
public static class LibellesStatuts
{
    private static readonly Dictionary<string, (string Libelle, TonaliteStatut Tonalite)> _statuts = new(StringComparer.Ordinal)
    {
        [StatutsFacture.Emise] = ("Émise", TonaliteStatut.Information),
        [StatutsFacture.Payee] = ("Payée", TonaliteStatut.Succes),
        [StatutsFacture.EnRetard] = ("En retard", TonaliteStatut.Danger),
        [StatutsAnalyse.Recu] = ("Reçu", TonaliteStatut.Neutre),
        [StatutsAnalyse.EnCours] = ("En cours", TonaliteStatut.Information),
        [StatutsAnalyse.Termine] = ("Terminé", TonaliteStatut.Succes),
        [nameof(ActionAudit.Creation)] = ("Création", TonaliteStatut.Succes),
        [nameof(ActionAudit.Modification)] = ("Modification", TonaliteStatut.Information),
        [nameof(ActionAudit.Suppression)] = ("Suppression", TonaliteStatut.Danger),
    };

    public static string Libelle(string? statut)
        => statut is not null && _statuts.TryGetValue(statut, out var valeur) ? valeur.Libelle : statut ?? string.Empty;

    public static TonaliteStatut Tonalite(string? statut)
        => statut is not null && _statuts.TryGetValue(statut, out var valeur) ? valeur.Tonalite : TonaliteStatut.Neutre;
}
