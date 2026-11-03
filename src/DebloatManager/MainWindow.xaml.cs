using DebloatManager.Services;
using DebloatManager.ViewModels;
using Wpf.Ui.Controls;

namespace DebloatManager;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();

        var viewModel = new MainViewModel(new WpfDialogService());
        DataContext = viewModel;
        Loaded += (_, _) => viewModel.ScanCommand.Execute(null);
    }
}
