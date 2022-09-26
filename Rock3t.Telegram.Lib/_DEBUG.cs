using System.Diagnostics;

namespace Rock3t.Telegram.Lib;

public static class _DEBUG
{
    public static string? GeneralConfigPath { get; private set; }
    public static string? ConfigPath { get; private set; }
    public static string? WordlistPath { get; private set; }

    [Conditional("DEBUG")]
    public static void SetDebugConfigPath(string debugPath)
    {
        GeneralConfigPath = debugPath;
        ConfigPath = Path.Combine(GeneralConfigPath, "appsettings.json");
        WordlistPath = Path.Combine(GeneralConfigPath, "wordlist.txt");
    }
}