using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.PlanningAnalyses;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.PlanningAnalyses;

/// <summary>Colonne de la grille : un jour du mois.</summary>
public sealed record JourPlanningViewModel(DateOnly Date, bool EstOuvre)
{
    public string Entete => $"{Date.Day}\n{Date.ToString("ddd", CultureInfo.CurrentCulture)}";
}

/// <summary>Case client × jour ; une case de jour non ouvré n'est pas modifiable.</summary>
public sealed partial class CasePlanningViewModel : ObservableObject
{
    private readonly Action _surModification;

    public CasePlanningViewModel(DateOnly date, bool estModifiable, string? typeAnalyse, Action surModification)
    {
        Date = date;
        EstModifiable = estModifiable;
        _surModification = surModification;
        TypeAnalyse = typeAnalyse;
    }

    public DateOnly Date { get; }

    public bool EstModifiable { get; }

    [ObservableProperty]
    public partial string? TypeAnalyse { get; set; }

    partial void OnTypeAnalyseChanged(string? oldValue, string? newValue)
    {
        if (oldValue != newValue)
        {
            _surModification();
        }
    }
}

/// <summary>Ligne de la grille : un client et ses cases du mois.</summary>
public sealed record LignePlanningViewModel(int IdClient, string RaisonSociale, IReadOnlyList<CasePlanningViewModel> Cases);

/// <summary>
/// Planning mensuel des analyses (ex-PlanningAnalyses/Index, îlot planning-analyses) : un client par ligne,
/// un jour par colonne, type d'analyse A à D sur les jours ouvrés ; sauvegarde du mois complet.
/// </summary>
public sealed partial class PlanningAnalysesViewModel : ViewModelBase, INavigable, IConfirmationDepart
{
    public const string MessageModificationsPerdues = "Les modifications non enregistrées seront perdues.";

    private readonly IExecuteurOperations _executeur;
    private readonly IServiceDialogues _dialogues;
    private readonly IServiceNotifications _notifications;

    public PlanningAnalysesViewModel(IExecuteurOperations executeur, IServiceDialogues dialogues, IServiceNotifications notifications, IAutorisation autorisation)
    {
        _executeur = executeur;
        _dialogues = dialogues;
        _notifications = notifications;
        PeutGerer = autorisation.APermission(Permissions.PlanningAnalyses.Gerer);
        TypesAnalyse = [new OptionFiltre<string>(null, "—"), .. TypesAnalysePlanning.Tous.Select(type => new OptionFiltre<string>(type, type))];
    }

    public override string Titre => "Planning des analyses";

    public bool PeutGerer { get; }

    public IReadOnlyList<OptionFiltre<string>> TypesAnalyse { get; }

    public ObservableCollection<LignePlanningViewModel> Lignes { get; } = [];

    [ObservableProperty]
    public partial IReadOnlyList<JourPlanningViewModel> Jours { get; private set; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LibelleMois))]
    public partial int Annee { get; private set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(LibelleMois))]
    public partial int Mois { get; private set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(SauvegarderCommand))]
    public partial bool EstModifie { get; private set; }

    [ObservableProperty]
    public partial bool EstVide { get; private set; }

    public string LibelleMois => Mois == 0 ? string.Empty : CultureInfo.CurrentCulture.TextInfo.ToTitleCase(new DateOnly(Annee, Mois, 1).ToString("MMMM yyyy", CultureInfo.CurrentCulture));

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ChargerMoisAsync(null, null);

    public bool PeutQuitter() => !EstModifie || _dialogues.Confirmer(MessageModificationsPerdues);

    [RelayCommand]
    private Task MoisPrecedentAsync() => ChangerDeMoisAsync(-1);

    [RelayCommand]
    private Task MoisSuivantAsync() => ChangerDeMoisAsync(1);

    [RelayCommand(CanExecute = nameof(PeutSauvegarder))]
    private Task SauvegarderAsync() => ExecuterOccupeAsync(async jeton =>
    {
        EffacerErreurs();
        var sauvegarde = new SauvegardePlanningDto
        {
            Annee = Annee,
            Mois = Mois,
            Cases = Lignes
                .SelectMany(ligne => ligne.Cases
                    .Where(caseJour => !string.IsNullOrEmpty(caseJour.TypeAnalyse))
                    .Select(caseJour => new CasePlanningDto { IdClient = ligne.IdClient, Date = caseJour.Date, TypeAnalyse = caseJour.TypeAnalyse! }))
                .ToList(),
        };

        var resultat = await _executeur.ExecuterAsync<IServicePlanningAnalyses, ResultatOperation>(
            (service, jetonOperation) => service.SauvegarderMoisAsync(sauvegarde, jetonOperation),
            jeton);
        if (!resultat.Reussi)
        {
            AfficherErreurs(resultat.Erreurs);
            return;
        }

        EstModifie = false;
        _notifications.Succes($"Le planning de {LibelleMois.ToLower(CultureInfo.CurrentCulture)} a été enregistré.");
    });

    private bool PeutSauvegarder() => PeutGerer && EstModifie;

    private Task ChangerDeMoisAsync(int decalage)
    {
        if (!PeutQuitter())
        {
            return Task.CompletedTask;
        }

        var mois = new DateOnly(Annee, Mois, 1).AddMonths(decalage);
        return ChargerMoisAsync(mois.Year, mois.Month);
    }

    private Task ChargerMoisAsync(int? annee, int? mois) => ExecuterOccupeAsync(async jeton =>
    {
        EffacerErreurs();
        var planning = await _executeur.ExecuterAsync<IServicePlanningAnalyses, PlanningMensuelDto>(
            (service, jetonOperation) => service.ObtenirMoisAsync(annee, mois, jetonOperation),
            jeton);

        var cases = planning.Cases.ToDictionary(caseJour => (caseJour.IdClient, caseJour.Date), caseJour => caseJour.TypeAnalyse);
        Lignes.Clear();
        foreach (var client in planning.Clients)
        {
            var casesClient = planning.Jours
                .Select(jour => new CasePlanningViewModel(
                    jour.Date,
                    jour.EstOuvre && PeutGerer,
                    cases.GetValueOrDefault((client.Id, jour.Date)),
                    MarquerModifie))
                .ToList();
            Lignes.Add(new LignePlanningViewModel(client.Id, client.RaisonSociale, casesClient));
        }

        Annee = planning.Annee;
        Mois = planning.Mois;
        Jours = planning.Jours.Select(jour => new JourPlanningViewModel(jour.Date, jour.EstOuvre)).ToList();
        EstVide = Lignes.Count == 0;
        EstModifie = false;
    });

    private void MarquerModifie() => EstModifie = true;
}
