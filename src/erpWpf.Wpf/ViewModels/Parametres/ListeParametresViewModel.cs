using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Autorisation;
using erpWeb.Core.Communs;
using erpWeb.Core.Parametres;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.Securite;
using erpWpf.Wpf.Services;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Parametres;

/// <summary>Paramètres en base (ex-Parametres/Index) : ils surchargent appsettings.json (notation Section:Cle).</summary>
public sealed partial class ListeParametresViewModel : ViewModelBase, INavigable
{
    private readonly IExecuteurOperations _executeur;
    private readonly IServiceNavigation _navigation;

    public ListeParametresViewModel(IExecuteurOperations executeur, IServiceNavigation navigation, IAutorisation autorisation)
    {
        _executeur = executeur;
        _navigation = navigation;
        PeutModifier = autorisation.APermission(Permissions.Parametres.Modifier);
    }

    public override string Titre => "Paramètres";

    public bool PeutModifier { get; }

    public ObservableCollection<ParametreDto> Parametres { get; } = [];

    public Task InitialiserAsync(string? parametre, CancellationToken jetonAnnulation) => ExecuterOccupeAsync(async jeton =>
    {
        var parametres = await _executeur.ExecuterAsync<IServiceParametres, IReadOnlyList<ParametreDto>>(
            (service, jetonOperation) => service.ListerAsync(jetonOperation),
            jeton);

        Parametres.Clear();
        foreach (var element in parametres)
        {
            Parametres.Add(element);
        }
    });

    [RelayCommand(CanExecute = nameof(PeutModifier))]
    private Task CreerAsync() => _navigation.NaviguerAsync(Destinations.CreationParametre, JetonEcran);

    [RelayCommand(CanExecute = nameof(PeutModifier))]
    private Task ModifierAsync(ParametreDto? parametre)
        => parametre is null ? Task.CompletedTask : _navigation.NaviguerAsync(Destinations.ModificationParametre(parametre.Cle), JetonEcran);
}
