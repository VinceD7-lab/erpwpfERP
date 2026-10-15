using System.Security.Claims;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Email;
using erpWeb.Core.Utilisateurs;
using erpWeb.UnitTests.Outils;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Moq;

namespace erpWeb.UnitTests.Core;

public sealed class ServiceAuthentificationTests : IAsyncLifetime
{
    private const string Email = "camille.martin@erpweb.local";
    private const string MotDePasse = "MotDePasse#2026";

    private HoteIdentiteTest _hote = default!;
    private Utilisateur _utilisateur = default!;

    private IServiceAuthentification Service => _hote.Services.GetRequiredService<IServiceAuthentification>();

    public async Task InitializeAsync()
    {
        _hote = await HoteIdentiteTest.CreerAsync(Mock.Of<IServiceEmail>());

        var role = await _hote.GestionnaireRoles.FindByNameAsync(RolesApplication.Utilisateur);
        await _hote.GestionnaireRoles.AddClaimAsync(role!, new Claim(Permissions.TypeClaim, Permissions.Clients.Lire));
        await _hote.GestionnaireRoles.AddClaimAsync(role!, new Claim(Permissions.TypeClaim, Permissions.Factures.Lire));

        _utilisateur = new Utilisateur { UserName = Email, Email = Email, NomComplet = "Camille Martin", EstActif = true };
        await _hote.GestionnaireUtilisateurs.CreateAsync(_utilisateur, MotDePasse);
        await _hote.GestionnaireUtilisateurs.AddToRoleAsync(_utilisateur, RolesApplication.Utilisateur);
    }

    public async Task DisposeAsync() => await _hote.DisposeAsync();

    [Fact]
    public async Task ConnecterAsync_IdentifiantsValides_RetourneLaSessionAvecLesPermissionsDesRoles()
    {
        var resultat = await Service.ConnecterAsync(Email, MotDePasse);

        Assert.True(resultat.Reussi, string.Join(" ", resultat.Erreurs));
        var session = resultat.Valeur!;
        Assert.Equal(_utilisateur.Id, session.IdUtilisateur);
        Assert.Equal("Camille Martin", session.NomComplet);
        Assert.Equal(new HashSet<string> { Permissions.Clients.Lire, Permissions.Factures.Lire }, session.Permissions);
        Assert.False(string.IsNullOrEmpty(session.TamponSecurite));
    }

    [Theory]
    [InlineData(Email, "Mauvais#2026")]
    [InlineData("inconnu@erpweb.local", MotDePasse)]
    [InlineData("", "")]
    public async Task ConnecterAsync_IdentifiantsInvalides_RetourneUnMessageGenerique(string email, string motDePasse)
    {
        var resultat = await Service.ConnecterAsync(email, motDePasse);

        Assert.False(resultat.Reussi);
        Assert.Equal([ServiceAuthentification.MessageIdentifiantsIncorrects], resultat.Erreurs);
    }

    [Fact]
    public async Task ConnecterAsync_EchecsRepetes_VerrouilleLeCompteMemeAvecLeBonMotDePasse()
    {
        for (var tentative = 1; tentative < HoteIdentiteTest.NombreEchecsAvantVerrouillage; tentative++)
        {
            await Service.ConnecterAsync(Email, "Mauvais#2026");
        }

        var dernierEchec = await Service.ConnecterAsync(Email, "Mauvais#2026");
        var avecBonMotDePasse = await Service.ConnecterAsync(Email, MotDePasse);

        Assert.Equal([ServiceAuthentification.MessageCompteVerrouille], dernierEchec.Erreurs);
        Assert.Equal([ServiceAuthentification.MessageCompteVerrouille], avecBonMotDePasse.Erreurs);
    }

    [Fact]
    public async Task ConnecterAsync_SuccesApresEchec_ReinitialiseLeCompteurDEchecs()
    {
        await Service.ConnecterAsync(Email, "Mauvais#2026");

        await Service.ConnecterAsync(Email, MotDePasse);

        Assert.Equal(0, await _hote.GestionnaireUtilisateurs.GetAccessFailedCountAsync(_utilisateur));
    }

    [Fact]
    public async Task ConnecterAsync_CompteDesactive_EstRefuse()
    {
        _utilisateur.EstActif = false;
        await _hote.GestionnaireUtilisateurs.UpdateAsync(_utilisateur);

        var resultat = await Service.ConnecterAsync(Email, MotDePasse);

        Assert.Equal([ServiceAuthentification.MessageCompteDesactive], resultat.Erreurs);
    }

    [Fact]
    public async Task ActualiserSessionAsync_SessionValide_RetourneLesPermissionsAJour()
    {
        var session = (await Service.ConnecterAsync(Email, MotDePasse)).Valeur!;
        await _hote.GestionnaireUtilisateurs.AddToRoleAsync(_utilisateur, RolesApplication.Administrateur);
        var roleAdministrateur = await _hote.GestionnaireRoles.FindByNameAsync(RolesApplication.Administrateur);
        await _hote.GestionnaireRoles.AddClaimAsync(roleAdministrateur!, new Claim(Permissions.TypeClaim, Permissions.Utilisateurs.Gerer));

        var actualisee = await Service.ActualiserSessionAsync(session.IdUtilisateur, session.TamponSecurite);

        Assert.NotNull(actualisee);
        Assert.Contains(Permissions.Utilisateurs.Gerer, actualisee.Permissions);
    }

    [Fact]
    public async Task ActualiserSessionAsync_CompteDesactive_RetourneNull()
    {
        var session = (await Service.ConnecterAsync(Email, MotDePasse)).Valeur!;
        await _hote.Services.GetRequiredService<IServiceUtilisateurs>().DefinirActivationAsync(_utilisateur.Id, estActif: false);

        Assert.Null(await Service.ActualiserSessionAsync(session.IdUtilisateur, session.TamponSecurite));
    }

    [Fact]
    public async Task ActualiserSessionAsync_TamponModifie_RetourneNull()
    {
        var session = (await Service.ConnecterAsync(Email, MotDePasse)).Valeur!;
        await _hote.GestionnaireUtilisateurs.UpdateSecurityStampAsync(_utilisateur);

        Assert.Null(await Service.ActualiserSessionAsync(session.IdUtilisateur, session.TamponSecurite));
    }
}
