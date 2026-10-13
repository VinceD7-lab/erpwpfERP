using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.TableauDeBord;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;

namespace erpWpf.Wpf.ViewModels.TableauDeBord;

/// <summary>Description d'un widget, lue une fois au chargement du tableau de bord.</summary>
public sealed record DescriptionWidget(string Nom, string Titre, string Icone, int Ordre, string? PermissionRequise);

/// <summary>
/// Carte du tableau de bord : charge et rafraîchit les données d'un widget (ex-WidgetViewComponent).
/// Le rendu dépend du type concret de <see cref="Donnees"/> (DataTemplate par type dans ModelesWidgets.xaml).
/// </summary>
public sealed partial class WidgetViewModel : ObservableObject
{
    public const string MessageEchec = "Rafraîchissement impossible.";

    private readonly IExecuteurOperations _executeur;
    private readonly IAutorisation _autorisation;
    private readonly IServiceNavigation _navigation;

    public WidgetViewModel(DescriptionWidget description, IExecuteurOperations executeur, IAutorisation autorisation, IServiceNavigation navigation)
    {
        Description = description;
        _executeur = executeur;
        _autorisation = autorisation;
        _navigation = navigation;
    }

    public DescriptionWidget Description { get; }

    public string Nom => Description.Nom;

    public string Titre => Description.Titre;

    public string Icone => Description.Icone;

    [ObservableProperty]
    public partial DonneesWidget? Donnees { get; private set; }

    [ObservableProperty]
    public partial bool EstOccupe { get; private set; }

    [ObservableProperty]
    public partial string? Erreur { get; private set; }

    [RelayCommand]
    public async Task RafraichirAsync(CancellationToken jetonAnnulation)
    {
        EstOccupe = true;
        Erreur = null;
        try
        {
            var donnees = await _executeur.ExecuterAsync<IEnumerable<IWidgetTableauDeBord>, DonneesWidget>(
                (widgets, jeton) => widgets.Single(widget => widget.Nom == Nom).ObtenirDonneesAsync(jeton),
                jetonAnnulation);
            Donnees = FiltrerRaccourcis(donnees);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            Erreur = MessageEchec;
        }
        finally
        {
            EstOccupe = false;
        }
    }

    [RelayCommand]
    private Task NaviguerAsync(string? destination)
        => string.IsNullOrEmpty(destination) ? Task.CompletedTask : _navigation.NaviguerAsync(destination);

    /// <summary>Chaque raccourci est masqué sans sa permission (ex-asp-permission sur DonneesRaccourcis.cshtml).</summary>
    private DonneesWidget FiltrerRaccourcis(DonneesWidget donnees)
        => donnees is DonneesRaccourcis raccourcis
            ? new DonneesRaccourcis(raccourcis.Raccourcis.Where(raccourci => _autorisation.APermission(raccourci.PermissionRequise)).ToList())
            : donnees;
}
