using System.Collections.ObjectModel;
using erpWeb.Core.TableauDeBord;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.TableauDeBord;

/// <summary>Tableau de bord (ex-TableauDeBord/Index) : widgets autorisés, triés par ordre, chargés en parallèle.</summary>
public sealed class TableauDeBordViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IAutorisation _autorisation;
    private readonly IServiceNavigation _navigation;
    private bool _estCharge;

    public TableauDeBordViewModel(IExecuteurOperations executeur, IAutorisation autorisation, IServiceNavigation navigation)
    {
        _executeur = executeur;
        _autorisation = autorisation;
        _navigation = navigation;
    }

    public override string Titre => "Tableau de bord";

    public ObservableCollection<WidgetViewModel> Widgets { get; } = [];

    public bool EstVide => _estCharge && Widgets.Count == 0;

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ExecuterOccupeAsync(async jeton =>
    {
        var descriptions = await _executeur.ExecuterAsync<IEnumerable<IWidgetTableauDeBord>, IReadOnlyList<DescriptionWidget>>(
            (widgets, _) => Task.FromResult<IReadOnlyList<DescriptionWidget>>(widgets
                .Select(widget => new DescriptionWidget(widget.Nom, widget.Titre, widget.Icone, widget.Ordre, widget.PermissionRequise))
                .ToList()),
            jeton);

        Widgets.Clear();
        foreach (var description in descriptions.Where(description => _autorisation.APermission(description.PermissionRequise)).OrderBy(description => description.Ordre))
        {
            Widgets.Add(new WidgetViewModel(description, _executeur, _autorisation, _navigation));
        }

        _estCharge = true;
        OnPropertyChanged(nameof(EstVide));
        await Task.WhenAll(Widgets.Select(widget => widget.RafraichirAsync(jeton)));
    });
}
