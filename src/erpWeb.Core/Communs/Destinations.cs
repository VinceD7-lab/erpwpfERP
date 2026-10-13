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

    public static string ModificationClient(int id) => $"Clients/Modifier/{id.ToString(CultureInfo.InvariantCulture)}";

    public static string TelechargementDocument(int id) => $"Documents/Telecharger/{id.ToString(CultureInfo.InvariantCulture)}";

    public static string ImpressionFacture(int id) => $"Factures/Imprimer/{id.ToString(CultureInfo.InvariantCulture)}";

    public static string ModificationUtilisateur(string id) => $"Utilisateurs/Modifier/{id}";

    public static string ModificationParametre(string cle) => $"Parametres/Modifier/{cle}";
}
