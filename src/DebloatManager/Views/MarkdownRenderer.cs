using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace DebloatManager.Views;

public static partial class MarkdownRenderer
{
    public static FlowDocument ToFlowDocument(string markdown)
    {
        var document = new FlowDocument
        {
            FontFamily = new FontFamily("Segoe UI"),
            FontSize = 13,
            PagePadding = new Thickness(16),
            TextAlignment = TextAlignment.Left,
            Foreground = (Brush)Application.Current.Resources["TextBrush"]
        };

        List? list = null;

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            if (line.Length == 0)
            {
                list = null;
                continue;
            }

            if (line.StartsWith("- ", StringComparison.Ordinal))
            {
                if (list is null)
                {
                    list = new List { Margin = new Thickness(0, 0, 0, 8), MarkerStyle = TextMarkerStyle.Disc };
                    document.Blocks.Add(list);
                }

                list.ListItems.Add(new ListItem(new Paragraph(new Run(CleanInline(line[2..]))) { Margin = new Thickness(0, 0, 0, 2) }));
                continue;
            }

            list = null;
            document.Blocks.Add(BuildParagraph(line));
        }

        return document;
    }

    private static Paragraph BuildParagraph(string line)
    {
        var level = line.TakeWhile(c => c == '#').Count();
        if (level == 0 || level >= line.Length || line[level] != ' ')
        {
            return new Paragraph(new Run(CleanInline(line))) { Margin = new Thickness(0, 0, 0, 8) };
        }

        return new Paragraph(new Run(CleanInline(line[(level + 1)..])))
        {
            FontSize = level == 1 ? 18 : 14,
            FontWeight = FontWeights.SemiBold,
            Margin = new Thickness(0, level == 1 ? 0 : 8, 0, 6)
        };
    }

    private static string CleanInline(string text)
        => LinkPattern().Replace(text, "$1").Replace("**", string.Empty).Replace("`", string.Empty);

    [GeneratedRegex(@"\[([^\]]+)\]\([^)]+\)")]
    private static partial Regex LinkPattern();
}
