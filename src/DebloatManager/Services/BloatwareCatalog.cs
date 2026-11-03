using System.IO.Enumeration;
using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed record CatalogEntry(string Pattern, string Category, RiskLevel Risk, string Notes, string? DisplayName = null);

public sealed record SystemEntry(string DisplayName, string Category, RiskLevel Risk, string Notes);

public static class BloatwareCatalog
{
    private const string Core = "Windows core";
    private const string Promo = "Promotional app";
    private const string MsApp = "Microsoft app";
    private const string Gaming = "Xbox and gaming";
    private const string Oem = "Manufacturer software";
    private const string Media = "Media app";
    private const string SelfInstalled = "May be preinstalled, or installed by you.";
    private const string NonRemovable = "Marked non-removable by Windows.";

    // Patterns are matched against the full package name, first match wins.
    public static readonly IReadOnlyList<CatalogEntry> StoreApps = new CatalogEntry[]
    {
        new("Microsoft.SecHealthUI", Core, RiskLevel.Protected, "The Windows Security interface.", "Windows Security"),
        new("Microsoft.UI.Xaml*", Core, RiskLevel.Protected, "UI framework used by many apps."),
        new("Microsoft.VCLibs*", Core, RiskLevel.Protected, "Runtime library used by many apps."),
        new("Microsoft.NET.*", Core, RiskLevel.Protected, ".NET runtime used by many apps."),

        new("Microsoft.WindowsStore", Core, RiskLevel.Risky, "Without it you cannot install or update Store apps, and it is hard to bring back.", "Microsoft Store"),
        new("Microsoft.StorePurchaseApp", Core, RiskLevel.Risky, "Handles purchases in the Microsoft Store.", "Store Purchase"),
        new("Microsoft.DesktopAppInstaller", Core, RiskLevel.Risky, "Provides winget and .appx installation.", "App Installer"),
        new("*WinAppRuntime*", Core, RiskLevel.Risky, "Runtime that other apps need in order to start.", "Windows App Runtime"),
        new("Microsoft.WindowsAppRuntime*", Core, RiskLevel.Risky, "Runtime that other apps need in order to start.", "Windows App Runtime"),
        new("Microsoft.MicrosoftEdge.Stable", Core, RiskLevel.Risky, "Edge integration package. Web content in Windows and other apps can break.", "Microsoft Edge"),
        new("MicrosoftCorporationII.WindowsSubsystemForLinux", Core, RiskLevel.Risky, "WSL. Linux distributions stop working.", "Windows Subsystem for Linux"),
        new("Microsoft.XboxGameCallableUI", Gaming, RiskLevel.Risky, NonRemovable + " Xbox sign-in dialogs in games can break.", "Xbox Game UI"),
        new("Microsoft.Windows.ContentDeliveryManager", Core, RiskLevel.Risky, NonRemovable + " Delivers suggested apps and Spotlight content.", "Content Delivery Manager"),
        new("Microsoft.Windows.ParentalControls", Core, RiskLevel.Risky, NonRemovable + " Family Safety limits stop working.", "Parental Controls"),
        new("Microsoft.Windows.NarratorQuickStart", Core, RiskLevel.Risky, NonRemovable + " Narrator tutorial.", "Narrator Quick Start"),
        new("Microsoft.Windows.PeopleExperienceHost", Core, RiskLevel.Risky, NonRemovable + " Taskbar contacts.", "People Experience Host"),
        new("Microsoft.MicrosoftEdgeDevToolsClient", Core, RiskLevel.Risky, NonRemovable + " Edge developer tools.", "Edge DevTools Client"),
        new("Microsoft.Windows.XGpuEjectDialog", Core, RiskLevel.Risky, NonRemovable + " External graphics eject dialog.", "External GPU Dialog"),

        new("Microsoft.BingNews", Promo, RiskLevel.Safe, "News app.", "News"),
        new("Microsoft.BingWeather", Promo, RiskLevel.Safe, "Weather app.", "Weather"),
        new("Microsoft.BingFinance", Promo, RiskLevel.Safe, "Finance app.", "Finance"),
        new("Microsoft.BingSports", Promo, RiskLevel.Safe, "Sports app.", "Sports"),
        new("Microsoft.GetHelp", Promo, RiskLevel.Safe, "Opens Microsoft support.", "Get Help"),
        new("Microsoft.Getstarted", Promo, RiskLevel.Safe, "Windows tips.", "Tips"),
        new("Microsoft.MicrosoftOfficeHub", Promo, RiskLevel.Safe, "Shortcut app that promotes Microsoft 365. It is not Office itself.", "Microsoft 365 Hub"),
        new("Microsoft.MicrosoftSolitaireCollection", Promo, RiskLevel.Safe, "Solitaire games with ads.", "Solitaire Collection"),
        new("Microsoft.MixedReality.Portal", Promo, RiskLevel.Safe, "Only needed for VR headsets.", "Mixed Reality Portal"),
        new("Microsoft.3DBuilder", Promo, RiskLevel.Safe, "3D modelling app.", "3D Builder"),
        new("Microsoft.Print3D", Promo, RiskLevel.Safe, "3D printing companion.", "Print 3D"),
        new("Microsoft.Microsoft3DViewer", Promo, RiskLevel.Safe, "3D model viewer.", "3D Viewer"),
        new("Microsoft.OneConnect", Promo, RiskLevel.Safe, "Mobile plans app.", "Mobile Plans"),
        new("Microsoft.Messaging", Promo, RiskLevel.Safe, "Retired messaging app.", "Messaging"),
        new("Microsoft.Wallet", Promo, RiskLevel.Safe, "Retired Microsoft Wallet.", "Wallet"),
        new("Microsoft.WindowsFeedbackHub", Promo, RiskLevel.Safe, "Sends feedback to Microsoft.", "Feedback Hub"),
        new("Microsoft.WindowsMaps", Promo, RiskLevel.Safe, "Maps app.", "Maps"),
        new("Microsoft.ZuneVideo", Promo, RiskLevel.Safe, "Movies and TV app.", "Movies & TV"),
        new("Microsoft.SkypeApp", Promo, RiskLevel.Safe, "Retired Skype client.", "Skype"),
        new("Clipchamp.Clipchamp", Promo, RiskLevel.Safe, "Preinstalled video editor.", "Clipchamp"),
        new("*CandyCrush*", Promo, RiskLevel.Safe, "Promotional game."),
        new("*BubbleWitch*", Promo, RiskLevel.Safe, "Promotional game."),
        new("*MarchofEmpires*", Promo, RiskLevel.Safe, "Promotional game."),
        new("*RoyalRevolt*", Promo, RiskLevel.Safe, "Promotional game."),
        new("*AsphaltNitro*", Promo, RiskLevel.Safe, "Promotional game."),
        new("*Asphalt8*", Promo, RiskLevel.Safe, "Promotional game."),
        new("*HiddenCity*", Promo, RiskLevel.Safe, "Promotional game."),
        new("*EclipseManager*", Promo, RiskLevel.Safe, "Promotional app."),
        new("*PhototasticCollage*", Promo, RiskLevel.Safe, "Promotional app."),
        new("*AutodeskSketchBook*", Promo, RiskLevel.Safe, "Promotional app."),

        new("SpotifyAB.SpotifyMusic", Media, RiskLevel.Caution, SelfInstalled, "Spotify"),
        new("*TikTok*", Media, RiskLevel.Caution, SelfInstalled),
        new("*Netflix*", Media, RiskLevel.Caution, SelfInstalled),
        new("Facebook.*", Media, RiskLevel.Caution, SelfInstalled),
        new("*Disney*", Media, RiskLevel.Caution, SelfInstalled),
        new("*LinkedInforWindows*", Media, RiskLevel.Caution, SelfInstalled),
        new("Microsoft.MinecraftUWP", Gaming, RiskLevel.Caution, "Keep it if you play Minecraft.", "Minecraft"),

        new("Microsoft.XboxSpeechToTextOverlay", Gaming, RiskLevel.Safe, "Xbox speech overlay.", "Xbox Speech Overlay"),
        new("Microsoft.GamingApp", Gaming, RiskLevel.Safe, "The Xbox app.", "Xbox"),
        new("Microsoft.XboxApp", Gaming, RiskLevel.Safe, "Old Xbox Console Companion.", "Xbox Console Companion"),
        new("Microsoft.XboxGamingOverlay", Gaming, RiskLevel.Caution, "Game Bar. Some games show a popup without it.", "Xbox Game Bar"),
        new("Microsoft.XboxGameOverlay", Gaming, RiskLevel.Caution, "In-game overlay used by Game Bar.", "Xbox Game Overlay"),
        new("Microsoft.XboxIdentityProvider", Gaming, RiskLevel.Caution, "Xbox sign-in, needed by Xbox-connected games.", "Xbox Identity Provider"),
        new("Microsoft.Xbox.TCUI", Gaming, RiskLevel.Caution, "Xbox friends and party UI.", "Xbox TCUI"),
        new("Microsoft.GamingServices", Gaming, RiskLevel.Caution, "Required by Game Pass and Store games.", "Gaming Services"),

        new("Microsoft.549981C3F5F10", MsApp, RiskLevel.Caution, "Cortana assistant.", "Cortana"),
        new("Microsoft.Copilot", MsApp, RiskLevel.Caution, "Copilot app.", "Copilot"),
        new("Microsoft.Windows.Ai.Copilot.Provider", MsApp, RiskLevel.Caution, "Copilot integration.", "Copilot Provider"),
        new("Microsoft.BingSearch", MsApp, RiskLevel.Caution, "Web results in Windows Search.", "Bing Search"),
        new("Microsoft.BingTranslator", MsApp, RiskLevel.Caution, "Translator app.", "Translator"),
        new("Microsoft.People", MsApp, RiskLevel.Caution, "Contacts, used by the Mail app.", "People"),
        new("microsoft.windowscommunicationsapps", MsApp, RiskLevel.Caution, "Mail and Calendar.", "Mail and Calendar"),
        new("Microsoft.OutlookForWindows", MsApp, RiskLevel.Caution, "New Outlook.", "Outlook"),
        new("MSTeams", MsApp, RiskLevel.Caution, "Microsoft Teams.", "Teams"),
        new("MicrosoftTeams", MsApp, RiskLevel.Caution, "Microsoft Teams.", "Teams"),
        new("Microsoft.YourPhone", MsApp, RiskLevel.Caution, "Phone Link.", "Phone Link"),
        new("MicrosoftWindows.CrossDevice", MsApp, RiskLevel.Caution, "Connects Windows to your phone.", "Cross Device"),
        new("Microsoft.Todos", MsApp, RiskLevel.Caution, "To Do.", "To Do"),
        new("Microsoft.MicrosoftStickyNotes", MsApp, RiskLevel.Caution, "Notes stay synced only if you signed in.", "Sticky Notes"),
        new("Microsoft.Office.OneNote", MsApp, RiskLevel.Caution, "OneNote.", "OneNote"),
        new("Microsoft.PowerAutomateDesktop", MsApp, RiskLevel.Caution, "Automation tool.", "Power Automate"),
        new("Microsoft.Whiteboard", MsApp, RiskLevel.Caution, "Whiteboard.", "Whiteboard"),
        new("Microsoft.MicrosoftJournal", MsApp, RiskLevel.Caution, "Journal.", "Journal"),
        new("Microsoft.Windows.DevHome*", MsApp, RiskLevel.Caution, "Developer dashboard.", "Dev Home"),
        new("Microsoft.WindowsTerminal", MsApp, RiskLevel.Caution, "Default terminal on Windows 11.", "Terminal"),
        new("Microsoft.WindowsCalculator", MsApp, RiskLevel.Caution, "Calculator.", "Calculator"),
        new("Microsoft.WindowsCamera", MsApp, RiskLevel.Caution, "Camera.", "Camera"),
        new("Microsoft.WindowsNotepad", MsApp, RiskLevel.Caution, "Notepad.", "Notepad"),
        new("Microsoft.WindowsSoundRecorder", MsApp, RiskLevel.Caution, "Sound Recorder.", "Sound Recorder"),
        new("Microsoft.WindowsAlarms", MsApp, RiskLevel.Caution, "Clock and alarms.", "Clock"),
        new("Microsoft.Windows.Photos", MsApp, RiskLevel.Caution, "Default photo viewer.", "Photos"),
        new("Microsoft.Paint", MsApp, RiskLevel.Caution, "Paint.", "Paint"),
        new("Microsoft.ScreenSketch", MsApp, RiskLevel.Caution, "Snipping Tool and the Win+Shift+S shortcut.", "Snipping Tool"),
        new("Microsoft.ZuneMusic", MsApp, RiskLevel.Caution, "Media Player.", "Media Player"),
        new("MicrosoftCorporationII.QuickAssist", MsApp, RiskLevel.Caution, "Remote help tool.", "Quick Assist"),
        new("MicrosoftCorporationII.MicrosoftFamily", MsApp, RiskLevel.Caution, "Family Safety.", "Family"),
        new("MicrosoftWindows.Client.WebExperience", MsApp, RiskLevel.Caution, "Windows 11 Widgets board.", "Widgets"),
        new("Microsoft.WidgetsPlatformRuntime", MsApp, RiskLevel.Caution, "Runtime for Widgets.", "Widgets Runtime"),
        new("Microsoft.StartExperiencesApp", MsApp, RiskLevel.Caution, "Start menu recommendations and widgets.", "Start Experiences"),
        new("Microsoft.Winget.Source", MsApp, RiskLevel.Caution, "Package index used by winget.", "Winget Source"),
        new("Microsoft.ApplicationCompatibilityEnhancements", MsApp, RiskLevel.Caution, "Improves compatibility of some apps.", "App Compatibility"),
        new("Microsoft.D3DMappingLayers", MsApp, RiskLevel.Caution, "OpenCL and OpenGL compatibility layer.", "OpenCL and OpenGL Layer"),
        new("Microsoft.*VideoExtension*", MsApp, RiskLevel.Caution, "Video codec. Some videos may no longer play."),
        new("Microsoft.*ImageExtension*", MsApp, RiskLevel.Caution, "Image codec. Some photos may no longer open."),
        new("Microsoft.WebMediaExtensions", MsApp, RiskLevel.Caution, "Web media codecs.", "Web Media Extensions"),

        new("*DellSupportAssist*", Oem, RiskLevel.Caution, "Dell diagnostics, useful for warranty support."),
        new("*DellDigitalDelivery*", Oem, RiskLevel.Safe, "Dell software delivery tool."),
        new("*DellCustomerConnect*", Oem, RiskLevel.Safe, "Dell registration prompt."),
        new("*HPJumpStart*", Oem, RiskLevel.Safe, "HP onboarding app."),
        new("*HPSupportAssistant*", Oem, RiskLevel.Caution, "HP diagnostics, useful for warranty support."),
        new("*myHP*", Oem, RiskLevel.Caution, "HP companion app."),
        new("*LenovoCompanion*", Oem, RiskLevel.Safe, "Lenovo companion app."),
        new("*LenovoVantage*", Oem, RiskLevel.Caution, "Lenovo system settings and updates."),
        new("*MyASUS*", Oem, RiskLevel.Caution, "ASUS system settings and updates."),
        new("*ArmouryCrate*", Oem, RiskLevel.Caution, "ASUS fan, lighting and performance control."),
        new("*AcerCollection*", Oem, RiskLevel.Safe, "Acer app store shortcut."),
        new("*AcerJumpStart*", Oem, RiskLevel.Safe, "Acer onboarding app."),
        new("*NVIDIAControlPanel*", Oem, RiskLevel.Caution, "Graphics settings for NVIDIA drivers."),
        new("*RealtekAudioControl*", Oem, RiskLevel.Caution, "Audio settings for Realtek drivers."),
        new("*Nahimic*", Oem, RiskLevel.Caution, "Audio enhancement tied to the audio driver."),
    };

    // Patterns are matched against the display name in Apps & Features, first match wins.
    public static readonly IReadOnlyList<CatalogEntry> Programs = new CatalogEntry[]
    {
        new("Microsoft Edge WebView2*", Core, RiskLevel.Risky, "Many apps display their interface with it and will not start without it."),
        new("Microsoft Edge", Core, RiskLevel.Risky, "Web content in Windows and other apps can break. Its uninstaller often refuses to run."),
        new("Microsoft Update Health Tools", Core, RiskLevel.Risky, "Keeps Windows Update healthy."),
        new("Microsoft Windows Application Compatibility*", Core, RiskLevel.Risky, "Compatibility fixes for installed programs."),
        new("Microsoft OneDrive", MsApp, RiskLevel.Caution, "Back up your files first. Files kept only online stay in the cloud but leave this PC."),
        new("Microsoft Teams*", MsApp, RiskLevel.Caution, "Microsoft Teams."),
        new("Microsoft 365*", MsApp, RiskLevel.Caution, "Office apps. A licence you paid for stays on your account."),
        new("Windows PC Health Check", Promo, RiskLevel.Safe, "Windows 11 eligibility checker."),
        new("WildTangent*", Promo, RiskLevel.Safe, "Trial games."),
        new("McAfee*", Oem, RiskLevel.Caution, "Antivirus. Check that Microsoft Defender or another antivirus is active afterwards."),
        new("Norton*", Oem, RiskLevel.Caution, "Antivirus. Check that Microsoft Defender or another antivirus is active afterwards."),
        new("CyberLink*", Oem, RiskLevel.Caution, "Media software bundled by manufacturers."),
        new("Dell SupportAssist*", Oem, RiskLevel.Caution, "Dell diagnostics, useful for warranty support."),
        new("Dell Digital Delivery*", Oem, RiskLevel.Safe, "Dell software delivery tool."),
        new("HP JumpStart*", Oem, RiskLevel.Safe, "HP onboarding app."),
        new("HP Support Assistant*", Oem, RiskLevel.Caution, "HP diagnostics, useful for warranty support."),
        new("Lenovo Vantage*", Oem, RiskLevel.Caution, "Lenovo system settings and updates."),
        new("ASUS Live Update*", Oem, RiskLevel.Caution, "ASUS driver updates."),
        new("Acer Collection*", Oem, RiskLevel.Safe, "Acer app store shortcut."),
    };

    public static readonly IReadOnlyDictionary<string, SystemEntry> Services =
        new Dictionary<string, SystemEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["DiagTrack"] = new("Connected User Experiences and Telemetry", "Telemetry", RiskLevel.Caution, "Sends diagnostic data to Microsoft."),
            ["dmwappushservice"] = new("WAP Push Message Routing", "Telemetry", RiskLevel.Caution, "Used for device management and telemetry."),
            ["diagsvc"] = new("Diagnostic Execution Service", "Telemetry", RiskLevel.Caution, "Windows troubleshooters use it."),
            ["WerSvc"] = new("Windows Error Reporting", "Telemetry", RiskLevel.Caution, "Crash reports are no longer collected or sent."),
            ["RetailDemo"] = new("Retail Demo Service", "Telemetry", RiskLevel.Safe, "Only used on store display units."),
            ["MapsBroker"] = new("Downloaded Maps Manager", "System utility", RiskLevel.Caution, "Used by the Maps app."),
            ["XblAuthManager"] = new("Xbox Live Auth Manager", Gaming, RiskLevel.Caution, "Xbox sign-in in games."),
            ["XblGameSave"] = new("Xbox Live Game Save", Gaming, RiskLevel.Caution, "Cloud saves in Xbox games."),
            ["XboxNetApiSvc"] = new("Xbox Live Networking", Gaming, RiskLevel.Caution, "Online play in Xbox games."),
            ["SysMain"] = new("SysMain", "Performance", RiskLevel.Caution, "Preloads apps. Apps can start slower on hard drives."),
            ["Fax"] = new("Fax", "System utility", RiskLevel.Safe, "Unused on most PCs."),
            ["WSearch"] = new("Windows Search", "System utility", RiskLevel.Risky, "Start menu and File Explorer search become slow or incomplete."),
            ["PrintNotify"] = new("Printer Extensions and Notifications", "System utility", RiskLevel.Caution, "Only needed if you print."),
            ["RemoteRegistry"] = new("Remote Registry", "System utility", RiskLevel.Safe, "Rarely needed, and safer when off."),
        };

    public static readonly IReadOnlyDictionary<string, SystemEntry> Features =
        new Dictionary<string, SystemEntry>(StringComparer.OrdinalIgnoreCase)
        {
            ["Internet-Explorer-Optional-amd64"] = new("Internet Explorer", "Legacy", RiskLevel.Safe, "Replaced by Edge."),
            ["WorkFolders-Client"] = new("Work Folders Client", "Legacy", RiskLevel.Safe, "Rarely used at home."),
            ["Printing-XPSServices-Features"] = new("XPS Services", "Printing", RiskLevel.Safe, "XPS printing support."),
            ["Printing-PrintToPDFServices-Features"] = new("Microsoft Print to PDF", "Printing", RiskLevel.Caution, "The Save as PDF printer disappears from every app."),
            ["MediaPlayback"] = new("Media Features", "Media", RiskLevel.Caution, "Media playback used by several apps."),
            ["WindowsMediaPlayer"] = new("Windows Media Player Legacy", "Media", RiskLevel.Safe, "Replaced by the new Media Player."),
            ["MicrosoftWindowsPowerShellV2"] = new("Windows PowerShell 2.0 Engine", "Legacy", RiskLevel.Caution, "Very old scripts may need it."),
            ["MicrosoftWindowsPowerShellV2Root"] = new("Windows PowerShell 2.0", "Legacy", RiskLevel.Caution, "Very old scripts may need it."),
            ["TelnetClient"] = new("Telnet Client", "Legacy", RiskLevel.Safe, "Rarely used."),
            ["TFTP"] = new("TFTP Client", "Legacy", RiskLevel.Safe, "Rarely used."),
            ["LegacyComponents"] = new("Legacy Components", "Legacy", RiskLevel.Caution, "DirectPlay, used by older games."),
            ["SMB1Protocol"] = new("SMB 1.0 File Sharing", "Legacy", RiskLevel.Caution, "Insecure, but very old NAS devices may need it."),
            ["Containers-DisposableClientVM"] = new("Windows Sandbox", "Virtualization", RiskLevel.Caution, "Keep it if you test software in isolation."),
            ["Microsoft-Hyper-V-All"] = new("Hyper-V", "Virtualization", RiskLevel.Risky, "Virtual machines, and some emulators and containers, stop working."),
            ["NetFx3"] = new(".NET Framework 3.5", "Legacy", RiskLevel.Caution, "Required by some older applications and games."),
        };

    private static readonly string[] SharedRuntimeKeywords =
    {
        "Redistributable", "Runtime", " SDK", "DirectX", "Visual C++", "Driver", ".NET", "Update for Microsoft"
    };

    public static CatalogEntry? ClassifyStoreApp(string packageName, string publisher, bool nonRemovable, string? signatureKind)
    {
        var match = FindMatch(StoreApps, packageName);
        if (match is not null) return match;

        var isSystemSigned = string.Equals(signatureKind, "System", StringComparison.OrdinalIgnoreCase);
        if (nonRemovable && isSystemSigned)
        {
            return new CatalogEntry(packageName, Core, RiskLevel.Protected, "Windows component that Windows itself marks as non-removable.");
        }

        if (nonRemovable || isSystemSigned)
        {
            return new CatalogEntry(packageName, Core, RiskLevel.Risky, "Windows treats this package as a system component.");
        }

        if (publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogEntry(packageName, MsApp, RiskLevel.Caution, "Microsoft app that is not in the catalog. Review it before removing.");
        }

        return null;
    }

    public static CatalogEntry? ClassifyProgram(string displayName, string publisher)
    {
        var match = FindMatch(Programs, displayName);
        if (match is not null) return match;

        if (SharedRuntimeKeywords.Any(keyword => displayName.Contains(keyword, StringComparison.OrdinalIgnoreCase)))
        {
            return new CatalogEntry(displayName, "Runtime or driver", RiskLevel.Caution, "Other programs or your hardware may depend on this.");
        }

        if (publisher.Contains("Microsoft", StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogEntry(displayName, MsApp, RiskLevel.Caution, "Microsoft program that is not in the catalog. Review it before removing.");
        }

        return null;
    }

    private static CatalogEntry? FindMatch(IEnumerable<CatalogEntry> entries, string value)
        => entries.FirstOrDefault(entry => FileSystemName.MatchesSimpleExpression(entry.Pattern, value, ignoreCase: true));
}
