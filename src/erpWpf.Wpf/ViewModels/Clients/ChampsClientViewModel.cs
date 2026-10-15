using CommunityToolkit.Mvvm.ComponentModel;
using erpWeb.Core.Clients;

namespace erpWpf.Wpf.ViewModels.Clients;

/// <summary>
/// Champs saisissables d'un client, partagés par le formulaire (création, modification) et l'édition en ligne
/// de la grille (ex-partielle _ChampsClient.cshtml).
/// </summary>
public partial class ChampsClientViewModel : ObservableObject, IChampsClient
{
    [ObservableProperty]
    public partial string RaisonSociale { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string? Adresse1 { get; set; }

    [ObservableProperty]
    public partial string? Adresse2 { get; set; }

    [ObservableProperty]
    public partial string? CodePostal { get; set; }

    [ObservableProperty]
    public partial string? Ville { get; set; }

    [ObservableProperty]
    public partial string? Pays { get; set; }

    [ObservableProperty]
    public partial string? Telephone1 { get; set; }

    [ObservableProperty]
    public partial string? Telephone2 { get; set; }

    [ObservableProperty]
    public partial string? Email { get; set; }

    public void Copier(IChampsClient source)
    {
        RaisonSociale = source.RaisonSociale;
        Adresse1 = source.Adresse1;
        Adresse2 = source.Adresse2;
        CodePostal = source.CodePostal;
        Ville = source.Ville;
        Pays = source.Pays;
        Telephone1 = source.Telephone1;
        Telephone2 = source.Telephone2;
        Email = source.Email;
    }

    public CreationClientDto VersCreation() => new()
    {
        RaisonSociale = RaisonSociale,
        Adresse1 = Adresse1,
        Adresse2 = Adresse2,
        CodePostal = CodePostal,
        Ville = Ville,
        Pays = Pays,
        Telephone1 = Telephone1,
        Telephone2 = Telephone2,
        Email = Email,
    };

    /// <summary>Tous les champs sont transmis : ModifierAsync remplace l'ensemble des valeurs du client.</summary>
    public ModificationClientDto VersModification(int id) => new()
    {
        Id = id,
        RaisonSociale = RaisonSociale,
        Adresse1 = Adresse1,
        Adresse2 = Adresse2,
        CodePostal = CodePostal,
        Ville = Ville,
        Pays = Pays,
        Telephone1 = Telephone1,
        Telephone2 = Telephone2,
        Email = Email,
    };
}
