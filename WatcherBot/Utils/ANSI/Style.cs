namespace WatcherBot.Utils.ANSI;

public enum Style
{
    Normal = 0,
    Bold = 1,
    Underline = 4,
}

public static class StyleExtensions
{
    public static int GetNumber(this Style style) => (int)style;

    public static string GetCode(this Style style) => $"\u001b[{style.GetNumber()}m";
}
