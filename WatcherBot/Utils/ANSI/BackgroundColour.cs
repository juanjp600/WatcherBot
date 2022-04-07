namespace WatcherBot.Utils.ANSI;

public enum BackgroundColour
{
    Navy = 40,
    Orange = 41,
    Gray = 42,
    LightGrey = 43,
    LighterGrey = 44,
    Indigo = 45,
    OtherGrey = 46,
    White = 47,
}

public static class BackgroundColourExtensions
{
    public static int GetNumber(this BackgroundColour colour) => (int)colour;

    public static string GetCode(this BackgroundColour colour) => $"\u001b[{colour.GetNumber()}m";
}
