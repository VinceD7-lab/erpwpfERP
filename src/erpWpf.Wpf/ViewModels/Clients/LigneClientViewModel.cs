using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using erpWeb.Core.Clients;

namespace erpWpf.Wpf.ViewModels.Clients;

/// <summary>
/// Ligne de la grille des clients, éditable en place (ex-_LigneClient / _LigneClientEdition) :
/// Échap annule (CancelEdit), la validation de la ligne (Entrée, changement de ligne) l'enregistre (EndEdit).
/// </summary>
public sealed partial class LigneClientViewModel : ChampsClientViewModel, IEditableObject
{
    private readonly Func<LigneClientViewModel, Task> _enregistrer;
    private IChampsClient? _instantane;

    public LigneClientViewModel(ClientDto client, Func<LigneClientViewModel, Task> enregistrer)
    {
        Id = client.Id;
        _enregistrer = enregistrer;
        Copier(ClientVersChamps(client));
    }

    public int Id { get; }

    /// <summary>Dernier enregistrement refusé : la ligne est signalée jusqu'au prochain enregistrement réussi.</summary>
    [ObservableProperty]
    public partial string? Erreur { get; set; }

    public void BeginEdit() => _instantane ??= VersCreation();

    public void CancelEdit()
    {
        if (_instantane is not null)
        {
            Copier(_instantane);
            _instantane = null;
        }
    }

    public void EndEdit()
    {
        if (_instantane is null)
        {
            return;
        }

        var modifiee = !Equivalent(_instantane, this);
        _instantane = null;
        if (modifiee)
        {
            _ = _enregistrer(this);
        }
    }

    public void Actualiser(ClientDto client)
    {
        Copier(ClientVersChamps(client));
        Erreur = null;
    }

    private static CreationClientDto ClientVersChamps(ClientDto client) => new()
    {
        RaisonSociale = client.RaisonSociale,
        Adresse1 = client.Adresse1,
        Adresse2 = client.Adresse2,
        CodePostal = client.CodePostal,
        Ville = client.Ville,
        Pays = client.Pays,
        Telephone1 = client.Telephone1,
        Telephone2 = client.Telephone2,
        Email = client.Email,
    };

    private static bool Equivalent(IChampsClient avant, IChampsClient apres)
        => avant.RaisonSociale == apres.RaisonSociale
            && avant.Adresse1 == apres.Adresse1
            && avant.Adresse2 == apres.Adresse2
            && avant.CodePostal == apres.CodePostal
            && avant.Ville == apres.Ville
            && avant.Pays == apres.Pays
            && avant.Telephone1 == apres.Telephone1
            && avant.Telephone2 == apres.Telephone2
            && avant.Email == apres.Email;
}
