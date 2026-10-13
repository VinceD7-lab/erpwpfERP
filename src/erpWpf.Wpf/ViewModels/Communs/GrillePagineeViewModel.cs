using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Communs;

namespace erpWpf.Wpf.ViewModels.Communs;

/// <summary>
/// Base des grilles paginées et triées côté serveur (factures, journal d'audit, échantillons) :
/// remplace la configuration Tabulator commune (composants/grille.js) de la version web.
/// Le tri n'accepte que les colonnes de l'énumération <typeparamref name="TChampTri"/> (liste blanche).
/// </summary>
public abstract partial class GrillePagineeViewModel<TElement, TChampTri> : ViewModelBase, INavigable
    where TChampTri : struct, Enum
{
    private readonly TChampTri _triParDefaut;
    private readonly bool _triDescendantParDefaut;
    private int _versionRecherche;

    protected GrillePagineeViewModel(int taillePage, TChampTri triParDefaut, bool triDescendantParDefaut, IReadOnlyList<int>? taillesPage = null)
    {
        _triParDefaut = triParDefaut;
        _triDescendantParDefaut = triDescendantParDefaut;
        TaillesPage = taillesPage ?? [25, 50, 100, 200];
        NumeroPage = 1;
        NombrePages = 1;
        TaillePage = taillePage;
        Tri = triParDefaut;
        TriDescendant = triDescendantParDefaut;
    }

    public ObservableCollection<TElement> Elements { get; } = [];

    public IReadOnlyList<int> TaillesPage { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resume))]
    [NotifyCanExecuteChangedFor(nameof(PagePrecedenteCommand), nameof(PageSuivanteCommand), nameof(PremierePageCommand), nameof(DernierePageCommand))]
    public partial int NumeroPage { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resume))]
    [NotifyCanExecuteChangedFor(nameof(PageSuivanteCommand), nameof(DernierePageCommand))]
    public partial int NombrePages { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Resume))]
    public partial int NombreTotal { get; private set; }

    [ObservableProperty]
    public partial int TaillePage { get; set; }

    [ObservableProperty]
    public partial TChampTri Tri { get; private set; }

    [ObservableProperty]
    public partial bool TriDescendant { get; private set; }

    public string Resume => NombreTotal == 0
        ? "Aucun élément"
        : $"Page {NumeroPage} sur {NombrePages} · {NombreTotal} élément(s)";

    public virtual Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ChargerAsync();

    /// <summary>Recherche d'une page : appel du service de Core correspondant.</summary>
    protected abstract Task<ResultatPagine<TElement>> RechercherAsync(int numeroPage, int taillePage, TChampTri tri, bool triDescendant, CancellationToken jetonAnnulation);

    /// <summary>Remet les filtres propres à la grille à leur valeur initiale.</summary>
    protected virtual void ReinitialiserFiltres()
    {
    }

    /// <summary>Appelé après le chargement d'une page (ex. regroupement des lignes).</summary>
    protected virtual void PageChargee()
    {
    }

    [RelayCommand]
    public Task ChargerAsync() => ChargerPageAsync(NumeroPage);

    [RelayCommand]
    public Task FiltrerAsync() => ChargerPageAsync(1);

    [RelayCommand]
    private Task ReinitialiserAsync()
    {
        ReinitialiserFiltres();
        Tri = _triParDefaut;
        TriDescendant = _triDescendantParDefaut;
        return ChargerPageAsync(1);
    }

    /// <summary>Tri demandé par un clic sur un en-tête ; une colonne hors liste blanche est ignorée.</summary>
    [RelayCommand]
    private Task TrierAsync(string? colonne)
    {
        if (!Enum.TryParse<TChampTri>(colonne, ignoreCase: false, out var champ) || !Enum.IsDefined(champ))
        {
            return Task.CompletedTask;
        }

        TriDescendant = EqualityComparer<TChampTri>.Default.Equals(champ, Tri) && !TriDescendant;
        Tri = champ;
        return ChargerPageAsync(1);
    }

    [RelayCommand(CanExecute = nameof(PeutReculer))]
    private Task PremierePageAsync() => ChargerPageAsync(1);

    [RelayCommand(CanExecute = nameof(PeutReculer))]
    private Task PagePrecedenteAsync() => ChargerPageAsync(NumeroPage - 1);

    [RelayCommand(CanExecute = nameof(PeutAvancer))]
    private Task PageSuivanteAsync() => ChargerPageAsync(NumeroPage + 1);

    [RelayCommand(CanExecute = nameof(PeutAvancer))]
    private Task DernierePageAsync() => ChargerPageAsync(NombrePages);

    partial void OnTaillePageChanged(int value)
    {
        if (NombreTotal > 0)
        {
            _ = ChargerPageAsync(1);
        }
    }

    private bool PeutReculer() => NumeroPage > 1;

    private bool PeutAvancer() => NumeroPage < NombrePages;

    private Task ChargerPageAsync(int numeroPage) => ExecuterOccupeAsync(async jeton =>
    {
        // Une réponse arrivée après une recherche plus récente est ignorée.
        var version = Interlocked.Increment(ref _versionRecherche);
        EffacerErreurs();

        var resultat = await RechercherAsync(Math.Max(numeroPage, 1), TaillePage, Tri, TriDescendant, jeton);
        if (version != Volatile.Read(ref _versionRecherche))
        {
            return;
        }

        Elements.Clear();
        foreach (var element in resultat.Elements)
        {
            Elements.Add(element);
        }

        NombreTotal = resultat.NombreTotal;
        NombrePages = resultat.NombrePages;
        NumeroPage = resultat.NumeroPage;
        PageChargee();
    });
}
