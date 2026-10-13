using erpWeb.Core.Audit;
using erpWeb.Core.Communs;
using erpWeb.Core.PlanningAnalyses;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.ViewModels.Documents;
using erpWpf.Wpf.ViewModels.JournalAudit;
using erpWpf.Wpf.ViewModels.Parametres;
using erpWpf.Wpf.ViewModels.PlanningAnalyses;

namespace erpWeb.IntegrationTests;

/// <summary>Journal d'audit, paramètres, documents et planning sur la base réelle.</summary>
public sealed class AdministrationTests : IClassFixture<HoteApplication>
{
    private readonly HoteApplication _hote;

    public AdministrationTests(HoteApplication hote)
    {
        _hote = hote;
    }

    [Fact]
    public async Task JournalAudit_ApresUneModification_RestitueLEntreeFiltreeParAction()
    {
        await _hote.ConnecterAdministrateurAsync();
        await DefinirParametreAsync($"Tests:Audit{Guid.NewGuid():N}", "valeur");
        var journal = await NaviguerAsync<JournalAuditViewModel>(Destinations.JournalAudit);

        journal.Action = ActionAudit.Creation;
        journal.Recherche = "Parametre";
        await journal.FiltrerAsync();

        Assert.NotEmpty(journal.Elements);
        Assert.All(journal.Elements, entree => Assert.Equal(ActionAudit.Creation, entree.Action));
        Assert.Contains(journal.Elements, entree => entree.Utilisateur == HoteApplication.EmailAdministrateur);
    }

    [Fact]
    public async Task JournalAudit_DateDeFin_InclutLaJourneeChoisie()
    {
        await _hote.ConnecterAdministrateurAsync();
        await DefinirParametreAsync($"Tests:Jour{Guid.NewGuid():N}", "valeur");
        var journal = await NaviguerAsync<JournalAuditViewModel>(Destinations.JournalAudit);

        journal.DateDebut = DateTime.Today;
        journal.DateFin = DateTime.Today;
        await journal.FiltrerAsync();

        Assert.NotEmpty(journal.Elements);
    }

    [Fact]
    public async Task JournalAudit_Export_EnregistreUnClasseurExcel()
    {
        await _hote.ConnecterAdministrateurAsync();
        var journal = await NaviguerAsync<JournalAuditViewModel>(Destinations.JournalAudit);

        await journal.ExporterCommand.ExecuteAsync(null);

        var chemin = _hote.Fichiers.DernierEnregistrement!;
        Assert.EndsWith(".xlsx", chemin, StringComparison.Ordinal);
        await using var fichier = File.OpenRead(chemin);
        var signature = new byte[2];
        await fichier.ReadExactlyAsync(signature);
        Assert.Equal("PK"u8.ToArray(), signature);
    }

    [Fact]
    public async Task Parametre_Enregistre_ApparaitDansLaListeEtSeModifieParSaCle()
    {
        await _hote.ConnecterAdministrateurAsync();
        var cle = $"Tests:Surcharge{Guid.NewGuid():N}";
        await DefinirParametreAsync(cle, "42");
        var liste = Assert.IsType<ListeParametresViewModel>(_hote.Obtenir<IServiceNavigation>().PageCourante);
        Assert.Contains(liste.Parametres, parametre => parametre.Cle == cle && parametre.Valeur == "42");

        var edition = await NaviguerAsync<EditionParametreViewModel>(Destinations.ModificationParametre(cle));

        Assert.False(edition.EstNouveau);
        Assert.Equal("42", edition.Valeur);
    }

    [Fact]
    public async Task Documents_DepotPuisTelechargement_RestitueLeContenu()
    {
        await _hote.ConnecterAdministrateurAsync();
        var nom = $"note-{Guid.NewGuid():N}.txt";
        _hote.Fichiers.FichierAOuvrir = _hote.Fichiers.Creer(nom, "Contenu du document");
        var depot = await NaviguerAsync<DepotDocumentViewModel>(Destinations.DepotDocument);

        depot.ChoisirFichierCommand.Execute(null);
        await depot.DeposerCommand.ExecuteAsync(null);
        var liste = Assert.IsType<ListeDocumentsViewModel>(_hote.Obtenir<IServiceNavigation>().PageCourante);
        var document = Assert.Single(liste.Documents, element => element.NomFichier == nom);
        File.Delete(_hote.Fichiers.FichierAOuvrir.Chemin);
        await liste.TelechargerCommand.ExecuteAsync(document);

        Assert.Equal("text/plain", document.TypeContenu);
        Assert.Equal("Contenu du document", await File.ReadAllTextAsync(_hote.Fichiers.DernierEnregistrement!));
    }

    [Fact]
    public async Task Planning_CaseSauvegardee_EstRelueAuRechargementDuMois()
    {
        await _hote.ConnecterAdministrateurAsync();
        await using (var contexte = _hote.CreerContexte(out var portee))
        {
            using var _ = portee;
            contexte.Clients.Add(new erpWeb.Core.Clients.Client { RaisonSociale = $"Planifié {Guid.NewGuid():N}" });
            await contexte.SaveChangesAsync();
        }

        var planning = await NaviguerAsync<PlanningAnalysesViewModel>(Destinations.PlanningAnalyses);
        var caseOuvree = planning.Lignes[0].Cases.First(caseJour => caseJour.EstModifiable);
        caseOuvree.TypeAnalyse = TypesAnalysePlanning.B;
        await planning.SauvegarderCommand.ExecuteAsync(null);
        Assert.Empty(planning.Erreurs);

        var rechargement = await NaviguerAsync<PlanningAnalysesViewModel>(Destinations.PlanningAnalyses);

        Assert.Equal(TypesAnalysePlanning.B, rechargement.Lignes[0].Cases.Single(caseJour => caseJour.Date == caseOuvree.Date).TypeAnalyse);
    }

    private async Task DefinirParametreAsync(string cle, string valeur)
    {
        var edition = await NaviguerAsync<EditionParametreViewModel>(Destinations.CreationParametre);
        edition.Cle = cle;
        edition.Valeur = valeur;
        await edition.EnregistrerCommand.ExecuteAsync(null);
        Assert.Empty(edition.Erreurs);
    }

    private async Task<T> NaviguerAsync<T>(string destination)
    {
        var navigation = _hote.Obtenir<IServiceNavigation>();
        await navigation.NaviguerAsync(destination);
        return Assert.IsType<T>(navigation.PageCourante);
    }
}
