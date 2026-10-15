using CommunityToolkit.Mvvm.Input;
using erpWeb.Core.Communs;
using erpWpf.Wpf.Navigation;
using erpWpf.Wpf.ViewModels.Communs;

namespace erpWpf.Wpf.ViewModels.Compte;

/// <summary>Affiché par la navigation quand l'écran demandé exige une permission absente (ex-Compte/AccesRefuse).</summary>
public sealed partial class AccesRefuseViewModel : ViewModelBase
{
    private readonly IServiceNavigation _navigation;

    public AccesRefuseViewModel(IServiceNavigation navigation)
    {
        _navigation = navigation;
    }

    public override string Titre => "Accès refusé";

    [RelayCommand]
    private Task RetournerAuTableauDeBordAsync() => _navigation.NaviguerAsync(Destinations.TableauDeBord, JetonEcran);
}
