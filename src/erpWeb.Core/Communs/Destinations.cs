using System.Globalization;

namespace erpWeb.Core.Communs;

/// <summary>
/// Clés de navigation vers les écrans de l'application, au format « Module/Action/identifiant ».
/// Neutres vis-à-vis de la présentation : la couche d'affichage les associe à ses écrans.
/// </summary>
public static class Destinations
{
    public const string TableauDeBord = "TableauDeBord";
    public const string Clients = "Clients";
    public const string CreationClient = "Clients/Creer";
    public const string Documents = "Documents";
    public const string DepotDocument = "Documents/Deposer";
    public const string Echantillons = "Echantillons";
    public const string Factures = "Factures";
    public const string PlanningAnalyses = "PlanningAnalyses";
    public const string Utilisateurs = "Utilisateurs";
    public const string CreationUtilisateur = "Utilisateurs/Creer";
    public const string JournalAudit = "JournalAudit";
    public const string Parametres = "Parametres";
    public const string CreationParametre = "Parametres/Modifier";

    // Routes paramétrées : le dernier segment de la destination est l'identifiant.
    public const string RouteModificationClient = "Clients/Modifier";
    public const string RouteTelechargementDocument = "Documents/Telecharger";
    public const string RouteImpressionFacture = "Factures/Imprimer";
    public const string RouteModificationUtilisateur = "Utilisateurs/Modifier";
    public const string RouteModificationParametre = "Parametres/Modifier";

    public static string ModificationClient(int id) => $"{RouteModificationClient}/{id.ToString(CultureInfo.InvariantCulture)}";

    public static string TelechargementDocument(int id) => $"{RouteTelechargementDocument}/{id.ToString(CultureInfo.InvariantCulture)}";

    public static string ImpressionFacture(int id) => $"{RouteImpressionFacture}/{id.ToString(CultureInfo.InvariantCulture)}";

    public static string ModificationUtilisateur(string id) => $"{RouteModificationUtilisateur}/{id}";

    public static string ModificationParametre(string cle) => $"{RouteModificationParametre}/{cle}";
}
