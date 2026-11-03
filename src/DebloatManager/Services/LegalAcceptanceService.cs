using System.IO;

namespace DebloatManager.Services;

public static class LegalAcceptanceService
{
    // Bump this when the EULA or Terms change in a way users must accept again.
    public const string CurrentVersion = "1";

    private static readonly string AcceptanceFile = Path.Combine(AppPaths.DataDirectory, "accepted-terms.txt");

    public static bool HasAccepted()
    {
        try
        {
            return File.Exists(AcceptanceFile) && File.ReadAllText(AcceptanceFile).Trim() == CurrentVersion;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            return false;
        }
    }

    public static void RecordAcceptance()
    {
        Directory.CreateDirectory(AppPaths.DataDirectory);
        File.WriteAllText(AcceptanceFile, CurrentVersion);
    }
}
