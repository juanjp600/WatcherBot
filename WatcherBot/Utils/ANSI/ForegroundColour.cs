using System;

namespace WatcherBot.Utils.ANSI;

public enum ForegroundColour
{
    Grey,
    Red,
    Green,
    Yellow,
    Blue,
    Pink,
    Cyan,
    White,
}

public static class ForegroundColourExtensions
{
    public static string GetCode(this ForegroundColour colour) =>
        colour switch {
            ForegroundColour.Grey   => "\u001b[30m",
            ForegroundColour.Red    => "\u001b[31m",
            ForegroundColour.Green  => "\u001b[32m",
            ForegroundColour.Yellow => "\u001b[33m",
            ForegroundColour.Blue   => "\u001b[34m",
            ForegroundColour.Pink   => "\u001b[35m",
            ForegroundColour.Cyan   => "\u001b[36m",
            ForegroundColour.White  => "\u001b[37m",
            var _                   => throw new ArgumentOutOfRangeException(nameof(colour), colour, null),
        };
}
