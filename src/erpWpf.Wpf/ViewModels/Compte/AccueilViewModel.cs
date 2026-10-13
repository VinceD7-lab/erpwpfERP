using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Compte;

/// <summary>Fenêtre d'accueil hors connexion : bascule entre connexion et inscription.</summary>
public sealed partial class AccueilViewModel : ViewModelBase
{
    public AccueilViewModel(ConnexionViewModel connexion, InscriptionViewModel inscription)
    {
        Connexion = connexion;
        Inscription = inscription;
        EcranCourant = connexion;
    }

    public ConnexionViewModel Connexion { get; }

    public InscriptionViewModel Inscription { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(EstConnexionAffichee))]
    public partial ViewModelBase EcranCourant { get; private set; }

    public bool EstConnexionAffichee => ReferenceEquals(EcranCourant, Connexion);

    /// <summary>Message affiché au retour sur l'accueil (ex. session expirée).</summary>
    [ObservableProperty]
    public partial string? Avertissement { get; set; }

    [RelayCommand]
    private void AfficherInscription() => EcranCourant = Inscription;

    [RelayCommand]
    private void AfficherConnexion() => EcranCourant = Connexion;

    protected override void SurFermeture()
    {
        Connexion.Dispose();
        Inscription.Dispose();
    }
}
