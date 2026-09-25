using System;
using System.IO;

namespace Zhsan.GameManager;

/// <summary>Explicit build-time isolation, never a fallback for normal startup failures.</summary>
internal static class MigrationCheck
{
#if SEASON_MIGRATION_CHECK
    internal static bool Enabled => true;
#else
    internal static bool Enabled => false;
#endif
    private static readonly object LogLock = new();
    internal static string RunRoot { get; } = Enabled
        ? Path.Combine(AppContext.BaseDirectory, "check-results", $"{DateTime.UtcNow:yyyyMMdd-HHmmss}-{Environment.ProcessId}")
        : string.Empty;
    internal static string UserRoot => Enabled ? Path.Combine(RunRoot, "user")
        : Path.Combine(UserRootBase(), "WorldOfTheThreeKingdoms");

    /// <summary>
    /// Host folder that holds the game's "WorldOfTheThreeKingdoms" directory ("My Documents" normally).
    /// Hosts without xdg-user-dirs (WSL, headless sessions) report that special folder as an
    /// empty string, which would degrade UserRoot to a relative path and can collide with the
    /// same-named launcher file in the working directory; fall back to HOME and stay absolute.
    /// </summary>
    private static string UserRootBase()
    {
        string root = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        if (string.IsNullOrEmpty(root))
        {
            root = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
        }
        if (string.IsNullOrEmpty(root))
        {
            root = Directory.GetCurrentDirectory();
        }
        return Path.GetFullPath(root);
    }

    internal static void Initialize()
    {
        if (!Enabled) return;
        Directory.CreateDirectory(UserRoot);
        Log("isolation", "Dedicated build; local data only; business services disabled.");
    }

    internal static string UserPath(string name)
    {
        if (Enabled && (Path.IsPathRooted(name) || name.Split('/', '\\').Any(p => p == "..")))
            throw new InvalidOperationException("User resource path escapes isolated storage.");
        Directory.CreateDirectory(UserRoot);
        return Path.Combine(UserRoot, name);
    }

    internal static void CheckFile(string path, bool write = false)
    {
        if (!Enabled) return;
        string full = Path.GetFullPath(path);
        bool Within(string root) => full.Equals(Path.GetFullPath(root), StringComparison.OrdinalIgnoreCase)
            || full.StartsWith(Path.GetFullPath(root) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase);
        if (!Within(RunRoot) && (write || !Within(Path.Combine(AppContext.BaseDirectory, "Content"))))
            throw new InvalidOperationException("File access outside the dedicated check directories.");
    }

    internal static void DemandBusinessAccess()
    {
        if (!Enabled) return;
        Log("blocked-network", "A business network or external browser call was rejected.");
        throw new InvalidOperationException("Business network access is disabled in this dedicated build.");
    }

    internal static void Log(string category, string message)
    {
        if (!Enabled) return;
        lock (LogLock)
        {
            Directory.CreateDirectory(RunRoot);
            File.AppendAllText(Path.Combine(RunRoot, "events.log"), $"{DateTime.UtcNow:O} [{category}] {message}{Environment.NewLine}");
        }
    }
}
