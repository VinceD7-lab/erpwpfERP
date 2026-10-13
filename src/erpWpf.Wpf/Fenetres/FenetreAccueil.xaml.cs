using System.Windows;
using erpWpf.Wpf.ViewModels.Compte;

namespace erpWpf.Wpf.Fenetres;

public partial class FenetreAccueil : Window
{
    public FenetreAccueil(AccueilViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
