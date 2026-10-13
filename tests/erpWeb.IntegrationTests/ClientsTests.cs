using erpWeb.Core.Clients;
using erpWeb.Core.Communs;
using erpWeb.Core.Echantillons;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.ViewModels.Clients;
using Microsoft.EntityFrameworkCore;

namespace erpWeb.IntegrationTests;

/// <summary>Écrans des clients sur la base réelle : édition en ligne, formulaire, détail et échantillons du client.</summary>
public sealed class ClientsTests : IClassFixture<HoteApplication>
{
    private readonly HoteApplication _hote;

    public ClientsTests(HoteApplication hote)
    {
        _hote = hote;
    }

    [Fact]
    public async Task EditionEnLigne_SaisieValide_EnregistreEtConserveLesChampsNonAffiches()
    {
        var id = await AjouterClientAsync(new Client { RaisonSociale = $"Acier {Guid.NewGuid():N}", Adresse2 = "Bâtiment B", Telephone2 = "0102030405" });
        await _hote.ConnecterAdministrateurAsync();
        var ligne = (await OuvrirListeAsync()).Clients.Single(client => client.Id == id);

        ligne.BeginEdit();
        ligne.Ville = "Grenoble";
        ligne.Telephone1 = "04 76 00 00 00";
        ligne.EndEdit();
        var client = await AttendreAsync(id, enregistre => enregistre.Ville == "Grenoble");

        Assert.Equal("Bâtiment B", client.Adresse2);
        Assert.Equal("0102030405", client.Telephone2);
        Assert.Null(ligne.Erreur);
        // La ligne est relue après enregistrement : le service a normalisé le téléphone.
        await AttendreAsync(() => ligne.Telephone1 == "0476000000");
    }

    [Fact]
    public async Task EditionEnLigne_RaisonSocialeVide_SignaleLaLigneSansModifierLaBase()
    {
        var raisonSociale = $"Bois {Guid.NewGuid():N}";
        var id = await AjouterClientAsync(new Client { RaisonSociale = raisonSociale });
        await _hote.ConnecterAdministrateurAsync();
        var ligne = (await OuvrirListeAsync()).Clients.Single(client => client.Id == id);

        ligne.BeginEdit();
        ligne.RaisonSociale = string.Empty;
        ligne.EndEdit();
        await AttendreAsync(() => ligne.Erreur is not null);

        await using var contexte = _hote.CreerContexte(out var portee);
        using var _ = portee;
        Assert.Equal(raisonSociale, (await contexte.Clients.SingleAsync(client => client.Id == id)).RaisonSociale);
    }

    [Fact]
    public async Task Selection_ClientAvecEchantillon_AfficheSesEchantillons()
    {
        var code = $"ECH-{Guid.NewGuid():N}";
        var id = await AjouterClientAsync(new Client { RaisonSociale = $"Maraîcher {Guid.NewGuid():N}" });
        await using (var contexte = _hote.CreerContexte(out var portee))
        {
            using var _ = portee;
            contexte.Echantillons.Add(new Echantillon
            {
                CodeBarresAnonyme = code,
                DatePrelevement = new DateTime(2026, 4, 1, 8, 0, 0, DateTimeKind.Utc),
                Filiere = "Maraichage",
                StatutAnalyse = StatutsAnalyse.EnCours,
                IdClient = id,
            });
            await contexte.SaveChangesAsync();
        }

        await _hote.ConnecterAdministrateurAsync();
        var liste = await OuvrirListeAsync();

        liste.ClientSelectionne = liste.Clients.Single(ligne => ligne.Id == id);
        await AttendreAsync(() => liste.Echantillons.Elements.Count > 0);

        Assert.Equal(code, Assert.Single(liste.Echantillons.Elements).CodeBarresAnonyme);
    }

    [Fact]
    public async Task Formulaire_ModificationDUnClient_ChargeEtEnregistreTousLesChamps()
    {
        var id = await AjouterClientAsync(new Client { RaisonSociale = $"Verger {Guid.NewGuid():N}", Pays = "France" });
        await _hote.ConnecterAdministrateurAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();
        await navigation.NaviguerAsync(Destinations.ModificationClient(id));
        var edition = Assert.IsType<EditionClientViewModel>(navigation.PageCourante);

        edition.Champs.CodePostal = "26000";
        await edition.EnregistrerCommand.ExecuteAsync(null);

        Assert.IsType<ListeClientsViewModel>(navigation.PageCourante);
        var client = await AttendreAsync(id, enregistre => enregistre.CodePostal == "26000");
        Assert.Equal("France", client.Pays);
    }

    [Fact]
    public async Task Formulaire_CreationInvalide_AfficheLesErreursSansQuitterLEcran()
    {
        await _hote.ConnecterAdministrateurAsync();
        var navigation = _hote.Obtenir<IServiceNavigation>();
        await navigation.NaviguerAsync(Destinations.CreationClient);
        var edition = Assert.IsType<EditionClientViewModel>(navigation.PageCourante);

        await edition.EnregistrerCommand.ExecuteAsync(null);

        Assert.NotEmpty(edition.Erreurs);
        Assert.Same(edition, navigation.PageCourante);
    }

    [Fact]
    public async Task Suppression_Confirmee_RetireLeClient()
    {
        var id = await AjouterClientAsync(new Client { RaisonSociale = $"Éphémère {Guid.NewGuid():N}" });
        await _hote.ConnecterAdministrateurAsync();
        var liste = await OuvrirListeAsync();
        _hote.Dialogues.Reponse = true;

        await liste.SupprimerCommand.ExecuteAsync(liste.Clients.Single(client => client.Id == id));

        Assert.DoesNotContain(liste.Clients, client => client.Id == id);
        await using var contexte = _hote.CreerContexte(out var portee);
        using var _ = portee;
        Assert.False(await contexte.Clients.AnyAsync(client => client.Id == id));
    }

    private async Task<ListeClientsViewModel> OuvrirListeAsync()
    {
        var navigation = _hote.Obtenir<IServiceNavigation>();
        await navigation.NaviguerAsync(Destinations.Clients);
        return Assert.IsType<ListeClientsViewModel>(navigation.PageCourante);
    }

    private async Task<int> AjouterClientAsync(Client client)
    {
        await using var contexte = _hote.CreerContexte(out var portee);
        using var _ = portee;
        contexte.Clients.Add(client);
        await contexte.SaveChangesAsync();
        return client.Id;
    }

    /// <summary>L'enregistrement d'une ligne est déclenché par EndEdit sans être attendu : on attend son effet.</summary>
    private async Task<Client> AttendreAsync(int id, Func<Client, bool> condition)
    {
        for (var tentative = 0; tentative < 50; tentative++)
        {
            await using var contexte = _hote.CreerContexte(out var portee);
            using var _ = portee;
            var client = await contexte.Clients.AsNoTracking().SingleAsync(enregistre => enregistre.Id == id);
            if (condition(client))
            {
                return client;
            }

            await Task.Delay(100);
        }

        throw new TimeoutException("Le client n'a pas été enregistré.");
    }

    private static async Task AttendreAsync(Func<bool> condition)
    {
        for (var tentative = 0; tentative < 50 && !condition(); tentative++)
        {
            await Task.Delay(100);
        }

        Assert.True(condition());
    }
}
