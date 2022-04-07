namespace WatcherBot.Utils.ANSI;

public static class Ansi
{
    public const string ResetCode = "\u001b[0m";

    public static string WithForegroundColour(this string s, ForegroundColour c) => $"{c.GetCode()}{s}{ResetCode}";

    public static string WithBackgroundColour(this string s, BackgroundColour c) => $"{c.GetCode()}{s}{ResetCode}";

    public static string WithStyle(this string s, Style c) => $"{c.GetCode()}{s}{ResetCode}";
}
