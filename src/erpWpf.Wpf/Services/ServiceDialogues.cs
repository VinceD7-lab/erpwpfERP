using System.Windows;

namespace erpWpf.Wpf.Services;

public sealed class ServiceDialogues : IServiceDialogues
{
    public bool Confirmer(string message, string titre = "Confirmation")
        => MessageBox.Show(FenetreActive(), message, titre, MessageBoxButton.YesNo, MessageBoxImage.Question, MessageBoxResult.No) == MessageBoxResult.Yes;

    public void Informer(string message, string titre = "Information")
        => MessageBox.Show(FenetreActive(), message, titre, MessageBoxButton.OK, MessageBoxImage.Information);

    private static Window FenetreActive()
        => Application.Current.Windows.OfType<Window>().FirstOrDefault(fenetre => fenetre.IsActive) ?? Application.Current.MainWindow;
}
