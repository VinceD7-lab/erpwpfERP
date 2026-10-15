using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Communs;
using erpWeb.Core.Documents;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Documents;

/// <summary>Dépôt d'un document (ex-Documents/Deposer) : la taille maximale est contrôlée par le validateur de Core.</summary>
public sealed partial class DepotDocumentViewModel : ViewModelBase
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceNotifications _notifications;
    private readonly IServiceFichiers _fichiers;

    public DepotDocumentViewModel(IExecuteurOperations executeur, IServiceNavigation navigation, IServiceNotifications notifications, IServiceFichiers fichiers)
    {
        _executeur = executeur;
        _navigation = navigation;
        _notifications = notifications;
        _fichiers = fichiers;
    }

    public override string Titre => "Déposer un document";

    [ObservableProperty]
    public partial string TypeEntite { get; set; } = "General";

    [ObservableProperty]
    public partial string IdEntite { get; set; } = "0";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(DeposerCommand))]
    public partial FichierLocal? Fichier { get; private set; }

    [RelayCommand]
    private void ChoisirFichier()
    {
        var fichier = _fichiers.ChoisirFichierAOuvrir();
        if (fichier is not null)
        {
            Fichier = fichier;
        }
    }

    [RelayCommand(CanExecute = nameof(FichierChoisi))]
    private Task DeposerAsync() => ExecuterOccupeAsync(async jeton =>
    {
        EffacerErreurs();
        var fichier = Fichier!;
        await using var contenu = _fichiers.OuvrirEnLecture(fichier.Chemin);
        var depot = new DepotDocumentDto
        {
            TypeEntite = TypeEntite,
            IdEntite = IdEntite,
            NomFichier = fichier.Nom,
            TypeContenu = TypesContenu.Deduire(fichier.Nom),
            Taille = fichier.Taille,
            Contenu = contenu,
        };

        var resultat = await _executeur.ExecuterAsync<IServiceDocuments, ResultatOperation<int>>(
            (service, jetonOperation) => service.DeposerAsync(depot, jetonOperation),
            jeton);
        if (!resultat.Reussi)
        {
            AfficherErreurs(resultat.Erreurs);
            return;
        }

        _notifications.Succes($"Le document « {fichier.Nom} » a été déposé.");
        await _navigation.NaviguerAsync(Destinations.Documents, jeton);
    });

    [RelayCommand]
    private Task AnnulerAsync() => _navigation.NaviguerAsync(Destinations.Documents, JetonEcran);

    private bool FichierChoisi() => Fichier is not null;
}
