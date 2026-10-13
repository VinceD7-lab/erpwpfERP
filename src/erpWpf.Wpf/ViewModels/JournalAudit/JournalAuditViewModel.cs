using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Audit;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.JournalAudit;

/// <summary>
/// Journal d'audit (ex-JournalAudit/Index, îlot tableau-journal-audit) : recherche paginée côté serveur
/// et export Excel des entrées récentes.
/// </summary>
public sealed partial class JournalAuditViewModel : GrillePagineeViewModel<EntreeJournalAuditDto, ChampTriJournalAudit>
{
    public const int NombreMaximumExporte = 10_000;

    private readonly IExecuteurOperations _executeur;
    private readonly IServiceFichiers _fichiers;
    private readonly IServiceNotifications _notifications;
    private readonly TimeProvider _horloge;

    public JournalAuditViewModel(
        IExecuteurOperations executeur,
        IServiceFichiers fichiers,
        IServiceNotifications notifications,
        IAutorisation autorisation,
        TimeProvider horloge)
        : base(CriteresJournalAudit.TaillePageParDefaut, ChampTriJournalAudit.Date, triDescendantParDefaut: true)
    {
        _executeur = executeur;
        _fichiers = fichiers;
        _notifications = notifications;
        _horloge = horloge;
        PeutExporter = autorisation.APermission(Permissions.JournalAudit.Exporter);
        Actions =
        [
            new OptionFiltre<ActionAudit?>(null, "Toutes les actions"),
            .. Enum.GetValues<ActionAudit>().Select(action => new OptionFiltre<ActionAudit?>(action, LibellesStatuts.Libelle(action.ToString()))),
        ];
    }

    public override string Titre => "Journal d'audit";

    public bool PeutExporter { get; }

    public IReadOnlyList<OptionFiltre<ActionAudit?>> Actions { get; }

    [ObservableProperty]
    public partial string? Recherche { get; set; }

    [ObservableProperty]
    public partial ActionAudit? Action { get; set; }

    [ObservableProperty]
    public partial DateTime? DateDebut { get; set; }

    [ObservableProperty]
    public partial DateTime? DateFin { get; set; }

    [RelayCommand(CanExecute = nameof(PeutExporter))]
    private Task ExporterAsync() => ExecuterOccupeAsync(async jeton =>
    {
        var entrees = await _executeur.ExecuterAsync<ILectureJournalAudit, IReadOnlyList<EntreeJournalAuditDto>>(
            (service, jetonOperation) => service.ListerRecentesAsync(NombreMaximumExporte, jetonOperation),
            jeton);

        await _executeur.ExecuterAsync<IExportJournalAudit>(
            async (export, jetonOperation) =>
            {
                var nom = $"journal-audit-{_horloge.GetUtcNow().ToString("yyyyMMdd-HHmm", CultureInfo.InvariantCulture)}{export.ExtensionFichier}";
                var chemin = _fichiers.ChoisirEmplacementEnregistrement(nom, $"Classeur Excel|*{export.ExtensionFichier}");
                if (chemin is null)
                {
                    return;
                }

                await using var fichier = _fichiers.CreerEnEcriture(chemin);
                await fichier.WriteAsync(export.Exporter(entrees), jetonOperation);
                _notifications.Succes($"{entrees.Count} entrée(s) exportée(s).");
            },
            jeton);
    });

    protected override Task<ResultatPagine<EntreeJournalAuditDto>> RechercherAsync(
        int numeroPage, int taillePage, ChampTriJournalAudit tri, bool triDescendant, CancellationToken jetonAnnulation)
    {
        var criteres = new CriteresJournalAudit
        {
            NumeroPage = numeroPage,
            TaillePage = taillePage,
            Recherche = string.IsNullOrWhiteSpace(Recherche) ? null : Recherche.Trim(),
            Action = Action,
            DateDebut = DateDebut is { } debut ? new DateTimeOffset(debut.Date) : null,
            // Borne exclue côté service : la journée de fin choisie est incluse.
            DateFin = DateFin is { } fin ? new DateTimeOffset(fin.Date.AddDays(1)) : null,
            Tri = tri,
            TriDescendant = triDescendant,
        };

        return _executeur.ExecuterAsync<IRechercheJournalAudit, ResultatPagine<EntreeJournalAuditDto>>(
            (service, jeton) => service.RechercherAsync(criteres, jeton),
            jetonAnnulation);
    }

    protected override void ReinitialiserFiltres()
    {
        Recherche = null;
        Action = null;
        DateDebut = null;
        DateFin = null;
    }
}
