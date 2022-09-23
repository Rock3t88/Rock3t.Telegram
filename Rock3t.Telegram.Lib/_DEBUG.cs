using System.Diagnostics;

namespace Rock3t.Telegram.Lib;

public static class _DEBUG
{
    public static string? GeneralConfigPath { get; private set; }
    public static string? ConfigPath { get; private set; }
    public static string? GroupRulesPath { get; private set; }
    public static string? QuestionsPath { get; private set; }
    public static string? WordlistPath { get; private set; }

    [Conditional("DEBUG")]
    public static void SetDebugConfigPath(string debugPath)
    {
        GeneralConfigPath = debugPath;
        ConfigPath = Path.Combine(GeneralConfigPath, "appsettings.json");
        QuestionsPath = Path.Combine(GeneralConfigPath, "questions.yml");
        GroupRulesPath = Path.Combine(GeneralConfigPath, "gruppenregeln.txt");
        WordlistPath = Path.Combine(GeneralConfigPath, "wordlist.txt");
    }
}
