namespace erpWpf.Wpf.Services;

/// <summary>Boîtes de dialogue modales : les ViewModels n'appellent jamais MessageBox directement.</summary>
public interface IServiceDialogues
{
    bool Confirmer(string message, string titre = "Confirmation");

    void Informer(string message, string titre = "Information");
}
