using System.Globalization;
using System.Windows;
using System.Windows.Markup;
using System.Windows.Threading;
using erpWeb.Infrastructure;
using erpWpf.Wpf.Fenetres;
using erpWpf.Wpf.Hote;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace erpWpf.Wpf;

/// <summary>
/// Point d'entrée : construit l'hôte (ConstructionHote), initialise la base puis confie l'enchaînement
/// des fenêtres au GestionnaireFenetres. Gère les erreurs non interceptées (ex-UseExceptionHandler).
/// </summary>
public partial class App : Application
{
    private IHost? _hote;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        AppliquerCultureFrancaise();

        try
        {
            _hote = ConstructionHote.CreerConstructeur(e.Args).Build();
            DispatcherUnhandledException += SurErreurNonGeree;
            TaskScheduler.UnobservedTaskException += SurTacheNonObservee;

            await _hote.StartAsync();

            var configuration = _hote.Services.GetRequiredService<IConfiguration>();
            await _hote.Services.InitialiserDonneesAsync(
                appliquerMigrations: configuration.GetValue<bool>("BaseDeDonnees:AppliquerMigrationsAuDemarrage"));

            if (_hote.Services.GetRequiredService<IHostEnvironment>().IsDevelopment())
            {
                await _hote.Services.SemerClientsDemonstrationAsync(quantite: 25);
                await _hote.Services.SemerEchantillonsDemonstrationAsync(nombreTournees: 15);
            }

            _hote.Services.GetRequiredService<GestionnaireFenetres>().Demarrer();
        }
        catch (Exception exception)
        {
            _hote?.Services.GetService<ILogger<App>>()?.LogCritical(exception, "Démarrage de l'application impossible.");
            MessageBox.Show(
                $"L'application n'a pas pu démarrer : {exception.Message}\n\nVérifiez la chaîne de connexion SQL Server (appsettings.json) et l'accès au serveur.",
                "erpWpf — Démarrage impossible",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown(1);
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _hote?.Services.GetService<GestionnaireFenetres>()?.Dispose();
        _hote?.Dispose();
        base.OnExit(e);
    }

    private static void AppliquerCultureFrancaise()
    {
        var culture = new CultureInfo("fr-FR");
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;

        // Formats des liaisons XAML (dates, montants) : WPF utilise en-US par défaut.
        FrameworkElement.LanguageProperty.OverrideMetadata(
            typeof(FrameworkElement),
            new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(culture.IetfLanguageTag)));
    }

    private void SurErreurNonGeree(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        var identifiant = Guid.NewGuid().ToString("N")[..12];
        _hote?.Services.GetService<ILogger<App>>()?.LogError(e.Exception, "Erreur non gérée {IdentifiantErreur}.", identifiant);

        var fenetre = new FenetreErreur(identifiant);
        if (MainWindow is { IsVisible: true })
        {
            fenetre.Owner = MainWindow;
        }

        fenetre.ShowDialog();
        e.Handled = true;
    }

    private void SurTacheNonObservee(object? sender, UnobservedTaskExceptionEventArgs e)
    {
        _hote?.Services.GetService<ILogger<App>>()?.LogError(e.Exception, "Tâche en échec non observée.");
        e.SetObserved();
    }
}
