using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Documents;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Documents;

/// <summary>Documents déposés (ex-Documents/Index) : filtre par entité, téléchargement, suppression.</summary>
public sealed partial class ListeDocumentsViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;
    private readonly IServiceDialogues _dialogues;
    private readonly IServiceNotifications _notifications;

    public ListeDocumentsViewModel(
        IExecuteurOperations executeur,
        IServiceNavigation navigation,
        IServiceDialogues dialogues,
        IServiceNotifications notifications,
        IAutorisation autorisation)
    {
        _executeur = executeur;
        _navigation = navigation;
        _dialogues = dialogues;
        _notifications = notifications;
        PeutDeposer = autorisation.APermission(Permissions.Documents.Deposer);
        PeutSupprimer = autorisation.APermission(Permissions.Documents.Supprimer);
    }

    public override string Titre => "Documents";

    public bool PeutDeposer { get; }

    public bool PeutSupprimer { get; }

    public ObservableCollection<DocumentDto> Documents { get; } = [];

    [ObservableProperty]
    public partial string? TypeEntite { get; set; }

    [ObservableProperty]
    public partial string? IdEntite { get; set; }

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => FiltrerAsync();

    [RelayCommand]
    private Task FiltrerAsync() => ExecuterOccupeAsync(async jeton =>
    {
        var typeEntite = string.IsNullOrWhiteSpace(TypeEntite) ? null : TypeEntite.Trim();
        var idEntite = string.IsNullOrWhiteSpace(IdEntite) ? null : IdEntite.Trim();
        var documents = await _executeur.ExecuterAsync<IServiceDocuments, IReadOnlyList<DocumentDto>>(
            (service, jetonOperation) => service.ListerAsync(typeEntite, idEntite, jetonOperation),
            jeton);

        Documents.Clear();
        foreach (var document in documents)
        {
            Documents.Add(document);
        }
    });

    [RelayCommand]
    private Task ReinitialiserAsync()
    {
        TypeEntite = null;
        IdEntite = null;
        return FiltrerAsync();
    }

    [RelayCommand(CanExecute = nameof(PeutDeposer))]
    private Task DeposerAsync() => _navigation.NaviguerAsync(Destinations.DepotDocument, JetonEcran);

    [RelayCommand]
    private Task TelechargerAsync(DocumentDto? document)
        => document is null ? Task.CompletedTask : _navigation.NaviguerAsync(Destinations.TelechargementDocument(document.Id), JetonEcran);

    [RelayCommand(CanExecute = nameof(PeutSupprimer))]
    private Task SupprimerAsync(DocumentDto? document) => ExecuterOccupeAsync(async jeton =>
    {
        if (document is null || !_dialogues.Confirmer($"Supprimer le document « {document.NomFichier} » ?"))
        {
            return;
        }

        var resultat = await _executeur.ExecuterAsync<IServiceDocuments, ResultatOperation>(
            (service, jetonOperation) => service.SupprimerAsync(document.Id, jetonOperation),
            jeton);
        if (!resultat.Reussi)
        {
            _notifications.Erreur(string.Join(" ", resultat.Erreurs));
            return;
        }

        Documents.Remove(document);
        _notifications.Succes($"Le document « {document.NomFichier} » a été supprimé.");
    });
}
