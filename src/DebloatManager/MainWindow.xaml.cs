using DebloatManager.Services;
using DebloatManager.ViewModels;
using Wpf.Ui.Controls;

namespace DebloatManager;

public partial class MainWindow : FluentWindow
{
    public MainWindow()
    {
        InitializeComponent();
        DataContext = new MainViewModel(new WpfDialogService());
    }
}
