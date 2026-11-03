using System.IO;
using System.Reflection;

namespace DebloatManager.Services;

public sealed record LegalDocument(string Title, string ResourceName)
{
    public string ReadText()
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName)
            ?? throw new InvalidOperationException($"Missing embedded document: {ResourceName}");
        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }
}

public static class LegalDocuments
{
    public static IReadOnlyList<LegalDocument> All { get; } = new LegalDocument[]
    {
        new("Risks", "Legal.RISKS.md"),
        new("EULA", "Legal.EULA.md"),
        new("Terms of Use", "Legal.TERMS.md"),
        new("Privacy Policy", "Legal.PRIVACY.md"),
        new("License", "Legal.LICENSE"),
        new("Third-party notices", "Legal.THIRD-PARTY-NOTICES.md"),
    };
}
