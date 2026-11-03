using System.IO;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using DebloatManager.Models;

namespace DebloatManager.Services;

public sealed record SignatureResult(OriginStatus Status, string? Signer, string? Problem);

public static partial class SignatureInspector
{
    private const uint NoUserInterface = 2;
    private const uint FileChoice = 1;
    private const uint RevocationCheckNone = 0x10;
    private const uint CacheOnlyRetrieval = 0x1000;

    private const uint NoSignature = 0x800B0100;
    private const uint SubjectFormUnknown = 0x800B0003;
    private const uint ProviderUnknown = 0x800B0001;
    private const uint BadDigest = 0x80096010;
    private const uint Expired = 0x800B0101;
    private const uint UntrustedRoot = 0x800B0109;

    private static readonly Guid VerifyAction = new("00AAC56B-CD44-11D0-8CC2-00C04FC295EE");

    // Checks offline only, so inspecting software never contacts any server.
    public static SignatureResult Inspect(string filePath)
    {
        if (!File.Exists(filePath)) return new SignatureResult(OriginStatus.NotChecked, null, null);

        var code = Verify(filePath);
        return code switch
        {
            0 => new SignatureResult(OriginStatus.Signed, ReadSigner(filePath), null),
            NoSignature or SubjectFormUnknown or ProviderUnknown => new SignatureResult(OriginStatus.Unsigned, null, null),
            BadDigest => new SignatureResult(OriginStatus.Invalid, ReadSigner(filePath), "The file was changed after it was signed, or it is damaged."),
            Expired => new SignatureResult(OriginStatus.Invalid, ReadSigner(filePath), "The signing certificate has expired."),
            UntrustedRoot => new SignatureResult(OriginStatus.Invalid, ReadSigner(filePath), "The signature comes from a certificate Windows does not trust."),
            _ => new SignatureResult(OriginStatus.Invalid, ReadSigner(filePath), $"Windows could not validate the signature (0x{code:X8}).")
        };
    }

    private static string? ReadSigner(string filePath)
    {
        try
        {
            using var certificate = new X509Certificate2(X509Certificate.CreateFromSignedFile(filePath));
            var name = certificate.GetNameInfo(X509NameType.SimpleName, false);
            return string.IsNullOrWhiteSpace(name) ? null : name;
        }
        catch (CryptographicException)
        {
            return null;
        }
    }

    private static uint Verify(string filePath)
    {
        var path = Marshal.StringToHGlobalUni(filePath);
        var fileInfoMemory = Marshal.AllocHGlobal(Marshal.SizeOf<FileInfoData>());
        try
        {
            Marshal.StructureToPtr(new FileInfoData
            {
                Size = (uint)Marshal.SizeOf<FileInfoData>(),
                FilePath = path
            }, fileInfoMemory, false);

            var data = new TrustData
            {
                Size = (uint)Marshal.SizeOf<TrustData>(),
                UiChoice = NoUserInterface,
                UnionChoice = FileChoice,
                File = fileInfoMemory,
                ProviderFlags = RevocationCheckNone | CacheOnlyRetrieval
            };

            var action = VerifyAction;
            return unchecked((uint)WinVerifyTrust(IntPtr.Zero, ref action, ref data));
        }
        finally
        {
            Marshal.FreeHGlobal(fileInfoMemory);
            Marshal.FreeHGlobal(path);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct FileInfoData
    {
        public uint Size;
        public IntPtr FilePath;
        public IntPtr FileHandle;
        public IntPtr KnownSubject;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct TrustData
    {
        public uint Size;
        public IntPtr PolicyCallbackData;
        public IntPtr SipClientData;
        public uint UiChoice;
        public uint RevocationChecks;
        public uint UnionChoice;
        public IntPtr File;
        public uint StateAction;
        public IntPtr StateData;
        public IntPtr UrlReference;
        public uint ProviderFlags;
        public uint UiContext;
        public IntPtr SignatureSettings;
    }

    [LibraryImport("wintrust.dll")]
    private static partial int WinVerifyTrust(IntPtr window, ref Guid action, ref TrustData data);
}
