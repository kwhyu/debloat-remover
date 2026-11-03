using System.Windows;
using Wpf.Ui.Controls;

namespace DebloatManager.Views;

public partial class FirstRunWindow : FluentWindow
{
    public FirstRunWindow()
    {
        InitializeComponent();
    }

    private void OnReadDocuments(object sender, RoutedEventArgs e) => new LegalWindow("EULA") { Owner = this }.ShowDialog();

    private void OnContinue(object sender, RoutedEventArgs e) => DialogResult = true;

    private void OnExit(object sender, RoutedEventArgs e) => DialogResult = false;
}
