using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed record CatalogEntry(string Keyword, string Category, RiskLevel Risk, string Notes);

public static class BloatwareCatalog
{
    // Keyword is matched case-insensitively against package family name / display name / publisher.
    public static readonly List<CatalogEntry> Entries = new()
    {
        // Xbox & gaming
        new("Xbox", "Xbox & Gaming", RiskLevel.Safe, "Xbox companion app, safe to remove if you don't use Xbox services."),
        new("GamingApp", "Xbox & Gaming", RiskLevel.Safe, "Xbox Gaming app."),
        new("XboxGameOverlay", "Xbox & Gaming", RiskLevel.Safe, "Xbox in-game overlay."),
        new("XboxGamingOverlay", "Xbox & Gaming", RiskLevel.Safe, "Xbox Game Bar overlay."),
        new("XboxIdentityProvider", "Xbox & Gaming", RiskLevel.Caution, "Used for Xbox sign-in, some games depend on it."),
        new("XboxSpeechToTextOverlay", "Xbox & Gaming", RiskLevel.Safe, "Xbox accessibility overlay."),
        new("Xbox.TCUI", "Xbox & Gaming", RiskLevel.Caution, "Xbox game chat UI component."),
        new("MicrosoftSolitaireCollection", "Games", RiskLevel.Safe, "Preinstalled Solitaire game."),
        new("CandyCrush", "Games", RiskLevel.Safe, "Third-party promotional game."),
        new("MarchofEmpires", "Games", RiskLevel.Safe, "Third-party promotional game."),
        new("Disney", "Games", RiskLevel.Safe, "Third-party promotional game."),
        new("BubbleWitch", "Games", RiskLevel.Safe, "Third-party promotional game."),
        new("RoyalRevolt", "Games", RiskLevel.Safe, "Third-party promotional game."),
        new("AsphaltNitro", "Games", RiskLevel.Safe, "Third-party promotional game."),
        new("Asphalt8", "Games", RiskLevel.Safe, "Third-party promotional game."),
        new("HiddenCity", "Games", RiskLevel.Safe, "Third-party promotional game."),
        new("MinecraftUWP", "Games", RiskLevel.Caution, "Minecraft for Windows, keep if you play it."),

        // Assistant / info apps
        new("549981C3F5F10", "Assistant", RiskLevel.Caution, "Cortana assistant."),
        new("Cortana", "Assistant", RiskLevel.Caution, "Cortana assistant."),
        new("Microsoft.Copilot", "Assistant", RiskLevel.Caution, "Windows Copilot app."),
        new("BingWeather", "Info & News", RiskLevel.Safe, "Weather widget app."),
        new("BingNews", "Info & News", RiskLevel.Safe, "News app."),
        new("BingFinance", "Info & News", RiskLevel.Safe, "Finance app."),
        new("BingSports", "Info & News", RiskLevel.Safe, "Sports app."),
        new("GetHelp", "System Utility", RiskLevel.Safe, "Redirects to Microsoft support site."),
        new("Getstarted", "System Utility", RiskLevel.Safe, "Windows tips app."),
        new("MicrosoftTips", "System Utility", RiskLevel.Safe, "Windows tips app."),
        new("WindowsFeedbackHub", "System Utility", RiskLevel.Safe, "Feedback reporting tool."),
        new("MixedReality.Portal", "Extended Reality", RiskLevel.Safe, "Mixed Reality portal, only needed for VR headsets."),
        new("3DBuilder", "Creativity", RiskLevel.Safe, "3D modeling app."),
        new("Print3D", "Creativity", RiskLevel.Safe, "3D printing companion app."),
        new("Microsoft3DViewer", "Creativity", RiskLevel.Safe, "3D model viewer."),
        new("OneConnect", "Connectivity", RiskLevel.Safe, "Mobile plans app, rarely used on desktops."),
        new("Wallet", "Connectivity", RiskLevel.Safe, "Microsoft Wallet, mostly unused."),
        new("WebExperience", "System Utility", RiskLevel.Caution, "Widgets board (Windows 11 Widgets)."),
        new("Family", "System Utility", RiskLevel.Safe, "Microsoft Family Safety app."),
        new("QuickAssist", "System Utility", RiskLevel.Safe, "Remote assistance app, rarely used by most users."),
        new("RemoteDesktop", "System Utility", RiskLevel.Caution, "Microsoft Remote Desktop client."),
        new("WindowsTerminal", "System Utility", RiskLevel.Caution, "Windows Terminal app, keep if you use the command line."),
        new("DevHome", "System Utility", RiskLevel.Caution, "Dev Home, mainly useful for developers."),

        // Communication / productivity
        new("People", "Communication", RiskLevel.Caution, "Contacts hub used by Mail app."),
        new("SkypeApp", "Communication", RiskLevel.Safe, "Skype UWP client."),
        new("YourPhone", "Communication", RiskLevel.Caution, "Phone Link, keep if you sync with an Android phone."),
        new("MicrosoftTeams", "Communication", RiskLevel.Caution, "Teams consumer app, preinstalled since Windows 11."),
        new("Teams", "Communication", RiskLevel.Caution, "Microsoft Teams chat icon integration."),
        new("Todos", "Productivity", RiskLevel.Caution, "Microsoft To Do app."),
        new("MicrosoftStickyNotes", "Productivity", RiskLevel.Caution, "Sticky Notes app."),
        new("MicrosoftOfficeHub", "Productivity", RiskLevel.Safe, "Office promotional shortcut app, not Office itself."),
        new("Office.OneNote", "Productivity", RiskLevel.Caution, "OneNote UWP client, keep if you use OneNote."),
        new("OutlookForWindows", "Productivity", RiskLevel.Caution, "New Outlook client."),
        new("PowerAutomateDesktop", "Productivity", RiskLevel.Caution, "Automation tool, keep if used."),
        new("Clipchamp", "Creativity", RiskLevel.Safe, "Preinstalled video editor."),
        new("MicrosoftWhiteboard", "Productivity", RiskLevel.Safe, "Digital whiteboard app."),
        new("MSPaint", "Creativity", RiskLevel.Caution, "Modern Paint app."),
        new("ScreenSketch", "Creativity", RiskLevel.Caution, "Snip & Sketch screenshot tool."),
        new("OneDrive", "Productivity", RiskLevel.Caution, "Cloud storage sync client, keep if you use OneDrive backup."),

        // Media
        new("ZuneMusic", "Media", RiskLevel.Safe, "Groove Music / Media Player app."),
        new("ZuneVideo", "Media", RiskLevel.Safe, "Movies & TV app."),
        new("WindowsMaps", "Media", RiskLevel.Safe, "Offline maps app."),
        new("Spotify", "Media", RiskLevel.Safe, "Preinstalled third-party Spotify shortcut."),
        new("TikTok", "Media", RiskLevel.Safe, "Preinstalled third-party app."),
        new("Netflix", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("Facebook", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("Instagram", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("PandoraMediaInc", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("Hulu", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("iHeartRadio", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("Flipboard", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("Twitter", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("LinkedInforWindows", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("Duolingo", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("PhototasticCollage", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("PicsArt-PhotoStudio", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("AutodeskSketchBook", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),
        new("EclipseManager", "Media", RiskLevel.Safe, "Preinstalled third-party app shortcut."),

        // OEM trialware and utilities
        new("McAfee", "Third-Party Trialware", RiskLevel.Safe, "OEM trial antivirus, safe to remove if using Windows Defender."),
        new("Norton", "Third-Party Trialware", RiskLevel.Safe, "OEM trial antivirus."),
        new("WildTangent", "Third-Party Trialware", RiskLevel.Safe, "Game trial platform."),
        new("Booking.com", "Third-Party Trialware", RiskLevel.Safe, "Preinstalled promotional app."),
        new("TripAdvisor", "Third-Party Trialware", RiskLevel.Safe, "Preinstalled promotional app."),
        new("ExpressVPN", "Third-Party Trialware", RiskLevel.Safe, "Preinstalled VPN trial."),
        new("AmazonAssistant", "Third-Party Trialware", RiskLevel.Safe, "Amazon shopping assistant."),
        new("Amazon.com", "Third-Party Trialware", RiskLevel.Safe, "Preinstalled Amazon shortcut."),
        new("eBay", "Third-Party Trialware", RiskLevel.Safe, "Preinstalled eBay shortcut."),
        new("DropboxOEM", "Third-Party Trialware", RiskLevel.Safe, "Preinstalled Dropbox promotion."),
        new("WPSOffice", "Third-Party Trialware", RiskLevel.Safe, "Third-party office suite trial."),
        new("CyberLink", "Third-Party Trialware", RiskLevel.Safe, "OEM media/DVD software trial."),

        // Dell
        new("DellSupportAssist", "OEM Utility", RiskLevel.Caution, "Dell hardware diagnostics, useful for warranty support."),
        new("DellDigitalDelivery", "OEM Utility", RiskLevel.Safe, "Dell software delivery tool."),
        new("DellCustomerConnect", "OEM Utility", RiskLevel.Safe, "Dell registration prompt."),
        new("DellOptimizer", "OEM Utility", RiskLevel.Caution, "Dell performance tuning tool."),
        new("DellUpdate", "OEM Utility", RiskLevel.Caution, "Dell driver update tool."),
        new("DellMobileConnect", "OEM Utility", RiskLevel.Safe, "Dell phone-to-PC companion app."),

        // HP
        new("HPJumpStart", "OEM Utility", RiskLevel.Safe, "HP onboarding promotional app."),
        new("HPSupportAssistant", "OEM Utility", RiskLevel.Caution, "HP hardware diagnostics, useful for warranty support."),
        new("HPSystemEventUtility", "OEM Utility", RiskLevel.Caution, "HP hotkey and system event handler."),
        new("HPAudioSwitch", "OEM Utility", RiskLevel.Caution, "HP audio device switcher."),
        new("HPRegistration", "OEM Utility", RiskLevel.Safe, "HP product registration prompt."),
        new("myHP", "OEM Utility", RiskLevel.Safe, "HP companion app."),

        // Lenovo
        new("LenovoVantage", "OEM Utility", RiskLevel.Caution, "Lenovo system management app."),
        new("LenovoUtility", "OEM Utility", RiskLevel.Caution, "Lenovo hardware utility."),
        new("LenovoCompanion", "OEM Utility", RiskLevel.Safe, "Lenovo promotional companion app."),
        new("LenovoWelcome", "OEM Utility", RiskLevel.Safe, "Lenovo onboarding app."),
        new("LenovoHotkey", "OEM Utility", RiskLevel.Risky, "Lenovo hotkey driver helper, may control fn keys."),

        // Asus
        new("ASUSLiveUpdate", "OEM Utility", RiskLevel.Caution, "ASUS driver update tool."),
        new("ASUSSplendidVideoEnhancementTechnology", "OEM Utility", RiskLevel.Safe, "ASUS display color enhancement utility."),
        new("MyASUS", "OEM Utility", RiskLevel.Caution, "ASUS system management app."),
        new("ArmourySoftware", "OEM Utility", RiskLevel.Caution, "ASUS ROG gaming utility."),

        // Acer
        new("AcerCollection", "OEM Utility", RiskLevel.Safe, "Acer app store shortcut."),
        new("AcerUEIP", "OEM Utility", RiskLevel.Safe, "Acer usage data collector."),
        new("AcerJumpStart", "OEM Utility", RiskLevel.Safe, "Acer onboarding promotional app."),

        // System / core (do not encourage removal, tag as risky/caution informationally)
        new("WindowsCalculator", "System Utility", RiskLevel.Caution, "Calculator app."),
        new("WindowsCamera", "System Utility", RiskLevel.Caution, "Camera app."),
        new("WindowsSoundRecorder", "System Utility", RiskLevel.Caution, "Voice recorder app."),
        new("WindowsAlarms", "System Utility", RiskLevel.Caution, "Alarms & Clock app."),
        new("Windows.Photos", "System Utility", RiskLevel.Caution, "Photos app."),
        new("WindowsStore", "System Core", RiskLevel.Risky, "Microsoft Store, removing breaks app installs from the Store."),
        new("SecHealthUI", "System Core", RiskLevel.Risky, "Windows Security UI, removing disables the Defender interface."),
        new("NcsiUwpApp", "System Core", RiskLevel.Risky, "Network connectivity status indicator."),
        new("VCLibs", "System Core", RiskLevel.Risky, "Runtime library required by many UWP apps."),
        new("NET.Native", "System Core", RiskLevel.Risky, ".NET Native runtime required by UWP apps."),
        new("StartMenuExperienceHost", "System Core", RiskLevel.Risky, "Powers the Start menu, do not remove."),
        new("ShellExperienceHost", "System Core", RiskLevel.Risky, "Powers Windows shell UI elements, do not remove."),
        new("PeopleExperienceHost", "System Core", RiskLevel.Risky, "Windows shell contact experience component."),
        new("ParentalControls", "System Core", RiskLevel.Risky, "Windows Family Safety integration component."),
    };

    public static CatalogEntry? Match(string identifier)
    {
        foreach (var entry in Entries)
        {
            if (identifier.Contains(entry.Keyword, StringComparison.OrdinalIgnoreCase))
            {
                return entry;
            }
        }

        return null;
    }

    // Structural fallback rules applied when no curated catalog entry matches, so risk
    // classification does not depend entirely on a hand-maintained keyword list.
    private static readonly string[] SharedRuntimeKeywords =
    {
        "Redistributable", "Runtime", " SDK", "DirectX", "WebView2", "Update for Microsoft",
        "Visual C++", "Security Update", "Driver", ".NET Framework", ".NET Core", ".NET Desktop", "Hotfix"
    };

    public static CatalogEntry? ClassifyUwpFallback(string name, string publisher, bool nonRemovable, string? signatureKind)
    {
        if (nonRemovable)
        {
            return new CatalogEntry(name, "System Core", RiskLevel.Risky,
                "Windows marks this package as non-removable; removal is blocked or unsupported by the OS.");
        }

        if (string.Equals(signatureKind, "System", StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogEntry(name, "System Core", RiskLevel.Risky,
                "Digitally signed as a Windows system component.");
        }

        if (publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogEntry(name, "Microsoft App", RiskLevel.Caution,
                "First-party Microsoft app not in the curated bloatware list — review before removing.");
        }

        return null;
    }

    public static CatalogEntry? ClassifyWin32Fallback(string displayName, string publisher)
    {
        if (SharedRuntimeKeywords.Any(keyword => displayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return new CatalogEntry(displayName, "Shared Runtime / Driver", RiskLevel.Caution,
                "Other installed applications or hardware drivers may depend on this component.");
        }

        if (publisher.Contains("Microsoft Corporation", StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogEntry(displayName, "Microsoft Program", RiskLevel.Caution,
                "Microsoft-published program not in the curated list — review before removing.");
        }

        return null;
    }

    // Known telemetry / diagnostics services commonly targeted by debloat tools.
    public static readonly Dictionary<string, (string Category, RiskLevel Risk, string Notes)> KnownServices = new(StringComparer.OrdinalIgnoreCase)
    {
        ["DiagTrack"] = ("Telemetry", RiskLevel.Caution, "Connected User Experiences and Telemetry service."),
        ["dmwappushservice"] = ("Telemetry", RiskLevel.Caution, "WAP push message routing service, used for telemetry."),
        ["diagsvc"] = ("Telemetry", RiskLevel.Caution, "Diagnostic Execution Service."),
        ["WerSvc"] = ("Telemetry", RiskLevel.Caution, "Windows Error Reporting service."),
        ["RetailDemo"] = ("Telemetry", RiskLevel.Safe, "Retail Demo Service, only used on store display units."),
        ["MapsBroker"] = ("System Utility", RiskLevel.Caution, "Downloaded Maps Manager."),
        ["XblAuthManager"] = ("Xbox & Gaming", RiskLevel.Caution, "Xbox Live Auth Manager."),
        ["XblGameSave"] = ("Xbox & Gaming", RiskLevel.Caution, "Xbox Live Game Save."),
        ["XboxNetApiSvc"] = ("Xbox & Gaming", RiskLevel.Caution, "Xbox Live Networking Service."),
        ["SysMain"] = ("Performance", RiskLevel.Caution, "Superfetch, preloads apps into memory. Safe to disable on SSDs."),
        ["Fax"] = ("System Utility", RiskLevel.Safe, "Fax service, unused on most modern PCs."),
        ["WSearch"] = ("System Utility", RiskLevel.Risky, "Windows Search indexer, disabling slows down file search."),
        ["PrintNotify"] = ("System Utility", RiskLevel.Caution, "Printer notification service, only needed if you print."),
        ["RemoteRegistry"] = ("System Utility", RiskLevel.Safe, "Remote registry access, disabled by default and rarely needed."),
    };

    // Optional Windows features frequently disabled by debloat tools.
    public static readonly Dictionary<string, (string Category, RiskLevel Risk, string Notes)> KnownOptionalFeatures = new(StringComparer.OrdinalIgnoreCase)
    {
        ["Internet-Explorer-Optional-amd64"] = ("System Feature", RiskLevel.Safe, "Legacy Internet Explorer engine, replaced by Edge."),
        ["WorkFolders-Client"] = ("System Feature", RiskLevel.Safe, "Work Folders sync client, rarely used at home."),
        ["Printing-XPSServices-Features"] = ("System Feature", RiskLevel.Safe, "XPS printing support."),
        ["MediaPlayback"] = ("System Feature", RiskLevel.Caution, "Media playback support used by several apps."),
        ["WindowsMediaPlayer"] = ("System Feature", RiskLevel.Safe, "Legacy Windows Media Player."),
        ["MicrosoftWindowsPowerShellV2"] = ("System Feature", RiskLevel.Risky, "Legacy PowerShell v2 engine, some old scripts may depend on it."),
        ["TelnetClient"] = ("System Feature", RiskLevel.Safe, "Telnet client, rarely used."),
        ["TFTP"] = ("System Feature", RiskLevel.Safe, "TFTP client, rarely used."),
        ["LegacyComponents"] = ("System Feature", RiskLevel.Caution, "Legacy component support for older applications."),
        ["SMB1Protocol"] = ("System Feature", RiskLevel.Safe, "Legacy and insecure file sharing protocol, safe to disable."),
        ["Containers-DisposableClientVM"] = ("System Feature", RiskLevel.Caution, "Windows Sandbox, keep if you use isolated test environments."),
        ["Microsoft-Hyper-V-All"] = ("System Feature", RiskLevel.Risky, "Hyper-V virtualization platform, disabling breaks any VMs and some emulators/containers."),
        ["NetFx3"] = ("System Feature", RiskLevel.Caution, ".NET Framework 3.5, required by some older applications and games."),
        ["Printing-PrintToPDFServices-Features"] = ("System Feature", RiskLevel.Safe, "Microsoft Print to PDF driver."),
    };
}
