using System.Windows;

namespace erpWpf.Wpf.Fenetres;

/// <summary>Erreur non gérée (ex-page Erreur/Index) : affiche l'identifiant consigné dans le journal.</summary>
public partial class FenetreErreur : Window
{
    public FenetreErreur(string identifiant)
    {
        InitializeComponent();
        ChampIdentifiant.Text = identifiant;
    }

    private void SurFermer(object sender, RoutedEventArgs e) => Close();
}
