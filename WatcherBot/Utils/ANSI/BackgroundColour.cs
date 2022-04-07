using System;

namespace WatcherBot.Utils.ANSI;

public enum BackgroundColour
{
    Navy,
    Orange,
    Gray,
    LightGrey,
    LighterGrey,
    Indigo,
    OtherGrey,
    White,
}

public static class BackgroundColourExtensions
{
    public static string GetCode(this BackgroundColour colour) =>
        colour switch {
            BackgroundColour.Navy        => "\u001b[40m",
            BackgroundColour.Orange      => "\u001b[41m",
            BackgroundColour.Gray        => "\u001b[42m",
            BackgroundColour.LightGrey   => "\u001b[43m",
            BackgroundColour.LighterGrey => "\u001b[44m",
            BackgroundColour.Indigo      => "\u001b[45m",
            BackgroundColour.OtherGrey   => "\u001b[46m",
            BackgroundColour.White       => "\u001b[47m",
            var _                        => throw new ArgumentOutOfRangeException(nameof(colour), colour, null),
        };
}
