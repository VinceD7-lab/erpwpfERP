using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Communs;
using erpWeb.Core.Utilisateurs;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Compte;

/// <summary>Création de son propre compte (ex-Compte/Inscription), suivie de la connexion automatique.</summary>
public sealed partial class InscriptionViewModel : ViewModelBase
{
    private readonly IExecuteurOperations _executeur;
    private readonly ISessionUtilisateur _session;

    public InscriptionViewModel(IExecuteurOperations executeur, ISessionUtilisateur session)
    {
        _executeur = executeur;
        _session = session;
    }

    public override string Titre => "Créer un compte";

    [ObservableProperty]
    public partial string NomComplet { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string MotDePasse { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string ConfirmationMotDePasse { get; set; } = string.Empty;

    [RelayCommand]
    private Task InscrireAsync() => ExecuterOccupeAsync(async jeton =>
    {
        EffacerErreurs();
        var inscription = new InscriptionDto
        {
            NomComplet = NomComplet,
            Email = Email,
            MotDePasse = MotDePasse,
            ConfirmationMotDePasse = ConfirmationMotDePasse,
        };

        var resultat = await _executeur.ExecuterAsync<IServiceUtilisateurs, ResultatOperation<string>>(
            (service, jetonOperation) => service.InscrireAsync(inscription, jetonOperation),
            jeton);
        if (!resultat.Reussi)
        {
            AfficherErreurs(resultat.Erreurs);
            return;
        }

        var connexion = await _executeur.ExecuterAsync<IServiceAuthentification, ResultatOperation<SessionDto>>(
            (service, jetonOperation) => service.ConnecterAsync(inscription.Email, inscription.MotDePasse, jetonOperation),
            jeton);
        MotDePasse = string.Empty;
        ConfirmationMotDePasse = string.Empty;

        if (connexion.Reussi)
        {
            _session.Ouvrir(connexion.Valeur!);
        }
        else
        {
            AfficherErreurs(connexion.Erreurs);
        }
    });
}
