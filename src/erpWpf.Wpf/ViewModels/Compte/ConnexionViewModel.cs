using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Communs;
using erpWeb.Core.Utilisateurs;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Compte;

/// <summary>Écran de connexion (ex-Compte/Connexion) : ouvre la session du poste en cas de succès.</summary>
public sealed partial class ConnexionViewModel : ViewModelBase
{
    private readonly IExecuteurOperations _executeur;
    private readonly ISessionUtilisateur _session;

    public ConnexionViewModel(IExecuteurOperations executeur, ISessionUtilisateur session)
    {
        _executeur = executeur;
        _session = session;
    }

    public override string Titre => "Connexion";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnecterCommand))]
    public partial string Email { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(ConnecterCommand))]
    public partial string MotDePasse { get; set; } = string.Empty;

    [RelayCommand(CanExecute = nameof(PeutSeConnecter))]
    private Task ConnecterAsync() => ExecuterOccupeAsync(async jeton =>
    {
        EffacerErreurs();
        var resultat = await _executeur.ExecuterAsync<IServiceAuthentification, ResultatOperation<SessionDto>>(
            (service, jetonOperation) => service.ConnecterAsync(Email, MotDePasse, jetonOperation),
            jeton);

        MotDePasse = string.Empty;
        if (resultat.Reussi)
        {
            _session.Ouvrir(resultat.Valeur!);
        }
        else
        {
            AfficherErreurs(resultat.Erreurs);
        }
    });

    private bool PeutSeConnecter() => !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrEmpty(MotDePasse);
}
