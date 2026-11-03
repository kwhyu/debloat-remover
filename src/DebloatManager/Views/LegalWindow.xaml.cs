using System.Reflection;
using System.Windows.Controls;
using DebloatManager.Services;
using Wpf.Ui.Controls;

namespace DebloatManager.Views;

public partial class LegalWindow : FluentWindow
{
    public LegalWindow(string? initialTitle = null)
    {
        InitializeComponent();

        var version = Assembly.GetExecutingAssembly().GetName().Version?.ToString(3);
        VersionText.Text = $"DebloatManager {version}\ngithub.com/kwhyu/debloat-remover";

        DocumentList.ItemsSource = LegalDocuments.All;
        DocumentList.SelectedItem = LegalDocuments.All.FirstOrDefault(d => d.Title == initialTitle) ?? LegalDocuments.All[0];
    }

    private void OnDocumentChanged(object sender, SelectionChangedEventArgs e)
    {
        if (DocumentList.SelectedItem is LegalDocument document)
        {
            Viewer.Document = MarkdownRenderer.ToFlowDocument(document.ReadText());
        }
    }
}
