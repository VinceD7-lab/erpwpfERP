namespace erpWpf.Wpf.Navigation;

/// <summary>Crée les écrans et actions de navigation par l'injection de dépendances (implémentée dans la composition root).</summary>
public interface IFabriqueViewModels
{
    object Creer(Type type);
}
