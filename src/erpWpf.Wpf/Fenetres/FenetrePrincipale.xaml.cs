using System.Windows;
using erpWpf.Wpf.ViewModels;

namespace erpWpf.Wpf.Fenetres;

public partial class FenetrePrincipale : Window
{
    private readonly FenetrePrincipaleViewModel _viewModel;

    public FenetrePrincipale(FenetrePrincipaleViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    public void Demarrer() => _viewModel.Demarrer();
}
