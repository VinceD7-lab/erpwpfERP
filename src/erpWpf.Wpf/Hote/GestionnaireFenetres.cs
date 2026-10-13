using System.Windows;
using erpWpf.Wpf.Fenetres;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.ViewModels;
using erpWpf.Wpf.ViewModels.Compte;
using Microsoft.Extensions.DependencyInjection;

namespace erpWpf.Wpf.Hote;

/// <summary>
/// Enchaîne les fenêtres selon la session : accueil (connexion) tant qu'aucune session n'est ouverte,
/// fenêtre principale ensuite ; retour à l'accueil à la déconnexion ou à l'expiration de la session.
/// </summary>
public sealed class GestionnaireFenetres : IDisposable
{
    public const string MessageSessionExpiree = "Votre session a été fermée : compte désactivé ou mot de passe modifié. Reconnectez-vous.";

    private readonly IServiceProvider _services;
    private readonly ISessionUtilisateur _session;
    private readonly SurveillanceSession _surveillance;
    private readonly IServiceNavigation _navigation;
    private FenetreAccueil? _accueil;
    private FenetrePrincipale? _principale;
    private bool _fermetureProgrammee;

    public GestionnaireFenetres(IServiceProvider services, ISessionUtilisateur session, SurveillanceSession surveillance, IServiceNavigation navigation)
    {
        _services = services;
        _session = session;
        _surveillance = surveillance;
        _navigation = navigation;
    }

    public void Demarrer()
    {
        _session.SessionModifiee += SurSessionModifiee;
        _surveillance.SessionExpiree += SurSessionExpiree;
        AfficherAccueil();
    }

    public void Dispose()
    {
        _session.SessionModifiee -= SurSessionModifiee;
        _surveillance.SessionExpiree -= SurSessionExpiree;
        _surveillance.Arreter();
    }

    private void SurSessionModifiee(object? sender, EventArgs e)
    {
        if (_session.EstOuverte && _principale is null)
        {
            AfficherPrincipale();
        }
        else if (!_session.EstOuverte && _principale is not null)
        {
            _surveillance.Arreter();
            _navigation.Reinitialiser();
            AfficherAccueil();
            Fermer(_principale);
            _principale = null;
        }
    }

    private void SurSessionExpiree(object? sender, EventArgs e)
    {
        if (_accueil?.DataContext is AccueilViewModel accueil)
        {
            accueil.Avertissement = MessageSessionExpiree;
        }
    }

    private void AfficherAccueil()
    {
        // Créées hors du suivi du conteneur racine (libérées à la fermeture de la fenêtre, voir FabriqueViewModels).
        var accueil = ActivatorUtilities.CreateInstance<AccueilViewModel>(
            _services,
            ActivatorUtilities.CreateInstance<ConnexionViewModel>(_services),
            ActivatorUtilities.CreateInstance<InscriptionViewModel>(_services));
        _accueil = ActivatorUtilities.CreateInstance<FenetreAccueil>(_services, accueil);
        _accueil.Closed += SurFenetreFermee;
        Application.Current.MainWindow = _accueil;
        _accueil.Show();
    }

    private void AfficherPrincipale()
    {
        _principale = ActivatorUtilities.CreateInstance<FenetrePrincipale>(
            _services,
            ActivatorUtilities.CreateInstance<FenetrePrincipaleViewModel>(_services));
        _principale.Closed += SurFenetreFermee;
        Application.Current.MainWindow = _principale;
        _principale.Show();
        _surveillance.Demarrer();
        _principale.Demarrer();

        if (_accueil is not null)
        {
            Fermer(_accueil);
            _accueil = null;
        }
    }

    private void Fermer(Window fenetre)
    {
        _fermetureProgrammee = true;
        try
        {
            fenetre.Close();
        }
        finally
        {
            _fermetureProgrammee = false;
        }
    }

    /// <summary>Fermeture par l'utilisateur (croix) : l'application s'arrête.</summary>
    private void SurFenetreFermee(object? sender, EventArgs e)
    {
        if (sender is Window { DataContext: IDisposable viewModel })
        {
            viewModel.Dispose();
        }

        if (!_fermetureProgrammee)
        {
            Application.Current.Shutdown();
        }
    }
}
