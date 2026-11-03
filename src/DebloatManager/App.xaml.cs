using System.Windows;
using System.Windows.Media;
using DebloatManager.Services;
using DebloatManager.Views;
using Wpf.Ui.Appearance;

namespace DebloatManager;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        DispatcherUnhandledException += (_, args) =>
        {
            new FileLogService().Append($"Unexpected error: {args.Exception}");
            MessageBox.Show(args.Exception.Message, "DebloatManager error", MessageBoxButton.OK, MessageBoxImage.Error);
            args.Handled = true;
        };

        // Replaces the Windows accent color so every control stays gray.
        ApplicationAccentColorManager.Apply(Color.FromRgb(0xB8, 0xB8, 0xB8), ApplicationTheme.Dark);

        if (!LegalAcceptanceService.HasAccepted())
        {
            var firstRun = new FirstRunWindow();
            if (firstRun.ShowDialog() != true)
            {
                Shutdown();
                return;
            }

            LegalAcceptanceService.RecordAcceptance();
        }

        ShutdownMode = ShutdownMode.OnMainWindowClose;
        MainWindow = new MainWindow();
        MainWindow.Show();
    }
}
