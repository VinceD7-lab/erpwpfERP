using erpWeb.Core.Clients;
using erpWeb.Core.Echantillons;
using erpWeb.Core.Factures;
using erpWeb.Infrastructure.Donnees;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace erpWeb.IntegrationTests;

/// <summary>Contraintes du schéma SQL Server réel sur les factures (index uniques, clés étrangères, contraintes CHECK).</summary>
public sealed class ContraintesFacturesTests : IClassFixture<HoteApplication>
{
    private readonly HoteApplication _hote;

    public ContraintesFacturesTests(HoteApplication hote)
    {
        _hote = hote;
    }

    [Fact]
    public async Task Enregistrement_NumeroFactureDejaUtilise_EstRefuseParLIndexUnique()
    {
        var numero = NouveauNumero();
        await AjouterFactureAsync(numero: numero);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AjouterFactureAsync(numero: numero));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("IX_Factures_NumeroFacture", erreurSql.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Enregistrement_DeuxFacturesPourUnEchantillon_EstRefuseParLIndexUnique()
    {
        var identifiantEchantillon = await AjouterEchantillonAvecClientAsync(NouveauCode());
        var identifiantClient = await ObtenirIdentifiantClientAsync(identifiantEchantillon);
        await AjouterFactureAsync(idEchantillon: identifiantEchantillon, idClient: identifiantClient);

        var exception = await Assert.ThrowsAsync<DbUpdateException>(
            () => AjouterFactureAsync(idEchantillon: identifiantEchantillon, idClient: identifiantClient));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("IX_Factures_IdEchantillon", erreurSql.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Suppression_ClientAvecFacture_EstRefuseParLaCleEtrangere()
    {
        var identifiantFacture = await AjouterFactureAsync();
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        var identifiantClient = await contexte.Factures.Where(f => f.Id == identifiantFacture).Select(f => f.IdClient).SingleAsync();

        var exception = await Assert.ThrowsAsync<SqlException>(
            () => contexte.Clients.Where(c => c.Id == identifiantClient).ExecuteDeleteAsync());

        Assert.Contains("FK_Factures_Clients_IdClient", exception.Message, StringComparison.Ordinal);
        Assert.True(await contexte.Factures.AnyAsync(f => f.Id == identifiantFacture));
        Assert.True(await contexte.Clients.AnyAsync(c => c.Id == identifiantClient));
    }

    [Fact]
    public async Task Suppression_EchantillonAvecFacture_EstRefuseParLaCleEtrangere()
    {
        var identifiantFacture = await AjouterFactureAsync();
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        var identifiantEchantillon = await contexte.Factures.Where(f => f.Id == identifiantFacture).Select(f => f.IdEchantillon).SingleAsync();

        var exception = await Assert.ThrowsAsync<SqlException>(
            () => contexte.Echantillons.Where(e => e.Id == identifiantEchantillon).ExecuteDeleteAsync());

        Assert.Contains("FK_Factures_Echantillons_IdEchantillon", exception.Message, StringComparison.Ordinal);
        Assert.True(await contexte.Factures.AnyAsync(f => f.Id == identifiantFacture));
        Assert.True(await contexte.Echantillons.AnyAsync(e => e.Id == identifiantEchantillon));
    }

    [Theory]
    [InlineData(-0.01, 0, 0)]
    [InlineData(0, -0.01, 0)]
    [InlineData(0, 0, -0.01)]
    public async Task Enregistrement_MontantNegatif_EstRefuseParLaContrainteCheck(double horsTaxe, double tva, double toutesTaxes)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AjouterFactureAsync(
            montantHorsTaxe: (decimal)horsTaxe,
            montantTva: (decimal)tva,
            montantToutesTaxes: (decimal)toutesTaxes));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("CK_Factures_Montants", erreurSql.Message, StringComparison.Ordinal);
    }

    [Fact]
    public async Task Enregistrement_MontantsNuls_EstAccepte()
    {
        var identifiant = await AjouterFactureAsync(montantHorsTaxe: 0m, montantTva: 0m, montantToutesTaxes: 0m);

        Assert.True(identifiant > 0);
    }

    [Theory]
    [InlineData(-0.01)]
    [InlineData(100.01)]
    public async Task Enregistrement_RemiseHorsBornes_EstRefuseeParLaContrainteCheck(double remise)
    {
        var exception = await Assert.ThrowsAsync<DbUpdateException>(() => AjouterFactureAsync(remise: (decimal)remise));

        var erreurSql = Assert.IsType<SqlException>(exception.InnerException);
        Assert.Contains("CK_Factures_Remise", erreurSql.Message, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData(0.0)]
    [InlineData(12.5)]
    [InlineData(100.0)]
    public async Task Enregistrement_RemiseDansLesBornesInclusives_EstAcceptee(double remise)
    {
        var identifiant = await AjouterFactureAsync(remise: (decimal)remise);

        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Equal((decimal)remise, (await contexte.Factures.SingleAsync(f => f.Id == identifiant)).Remise);
    }

    [Fact]
    public async Task Enregistrement_RemiseAbsente_EstAcceptee()
    {
        var identifiant = await AjouterFactureAsync(remise: null);

        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        Assert.Null((await contexte.Factures.SingleAsync(f => f.Id == identifiant)).Remise);
    }

    private async Task<bool> ExisteFactureAsync(int idEchantillon)
    {
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        return await contexte.Factures.AnyAsync(facture => facture.IdEchantillon == idEchantillon);
    }

    private static string NouveauCode() => $"ECH-{Guid.NewGuid():N}";

    /// <summary>Numéro éloigné de la séquence réelle pour ne pas entrer en collision avec les factures générées par d'autres tests.</summary>
    private static int NouveauNumero() => Random.Shared.Next(100_000_000, 1_000_000_000);

    private async Task<int> AjouterEchantillonAvecClientAsync(string code)
    {
        using var portee = _hote.Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<AppDbContext>();
        var echantillon = new Echantillon
        {
            CodeBarresAnonyme = code,
            DatePrelevement = new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc),
            Filiere = "Elevage",
            StatutAnalyse = StatutsAnalyse.Recu,
            Client = new Client { RaisonSociale = $"Client {Guid.NewGuid():N}", Ville = "Lyon" },
        };
        contexte.Echantillons.Add(echantillon);
        await contexte.SaveChangesAsync();
        return echantillon.Id;
    }

    private async Task<int> ObtenirIdentifiantClientAsync(int idEchantillon)
    {
        await using var contexte = CreerContexte(out var portee);
        using var _ = portee;
        return await contexte.Echantillons.Where(e => e.Id == idEchantillon).Select(e => e.IdClient!.Value).SingleAsync();
    }

    /// <summary>Insère une facture (et, sauf indication contraire, son client et son échantillon) et renvoie son identifiant.</summary>
    private async Task<int> AjouterFactureAsync(
        int? numero = null,
        int? idEchantillon = null,
        int? idClient = null,
        string? nomClient = null,
        DateOnly? dateFacture = null,
        decimal montantHorsTaxe = 45m,
        decimal montantTva = 9m,
        decimal montantToutesTaxes = 54m,
        decimal? remise = null)
    {
        using var portee = _hote.Services.CreateScope();
        var contexte = portee.ServiceProvider.GetRequiredService<AppDbContext>();
        var facture = new Facture
        {
            NumeroFacture = numero ?? NouveauNumero(),
            DateFacture = dateFacture ?? new DateOnly(2026, 3, 4),
            DateEcheance = (dateFacture ?? new DateOnly(2026, 3, 4)).AddDays(30),
            NomClient = nomClient ?? "Acier SA",
            MontantHorsTaxe = montantHorsTaxe,
            TauxTva = 20m,
            MontantTva = montantTva,
            MontantToutesTaxesComprises = montantToutesTaxes,
            Remise = remise,
            Devise = "EUR",
            StatutFacture = StatutsFacture.Emise,
        };

        if (idClient is { } identifiantClient)
        {
            facture.IdClient = identifiantClient;
        }
        else
        {
            facture.Client = new Client { RaisonSociale = nomClient ?? "Acier SA" };
        }

        if (idEchantillon is { } identifiantEchantillon)
        {
            facture.IdEchantillon = identifiantEchantillon;
        }
        else
        {
            facture.Echantillon = new Echantillon
            {
                CodeBarresAnonyme = NouveauCode(),
                DatePrelevement = new DateTime(2026, 3, 3, 9, 0, 0, DateTimeKind.Utc),
                Filiere = "Elevage",
                StatutAnalyse = StatutsAnalyse.Recu,
            };
        }

        contexte.Factures.Add(facture);
        await contexte.SaveChangesAsync();
        return facture.Id;
    }

    private AppDbContext CreerContexte(out IServiceScope portee)
    {
        portee = _hote.Services.CreateScope();
        return portee.ServiceProvider.GetRequiredService<AppDbContext>();
    }
}
