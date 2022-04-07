namespace WatcherBot.Utils.ANSI;

public static class Ansi
{
    public const string ResetCode = "\u001b[0m";

    public static string WithForegroundColour(this string str, ForegroundColour c) => $"{c.GetCode()}{str}{ResetCode}";

    public static string WithBackgroundColour(this string str, BackgroundColour c) => $"{c.GetCode()}{str}{ResetCode}";

    public static string WithStyle(this string str, Style s) => $"{s.GetCode()}{str}{ResetCode}";

    public static string WithForegroundColourAndStyle(this string str, ForegroundColour c, Style s) =>
        $"\u001b[{c.GetNumber()};{s.GetNumber()}m{str}{ResetCode}";

    public static string WithOptionalForegroundColourAndStyle(this string str, ForegroundColour? c, Style? s)
    {
        if (c.HasValue && s.HasValue) { return str.WithForegroundColourAndStyle(c.Value, s.Value); }

        if (c.HasValue) { return str.WithForegroundColour(c.Value); }

        if (s.HasValue) { return str.WithStyle(s.Value); }

        return str;
    }
}
