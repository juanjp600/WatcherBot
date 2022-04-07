using System;

namespace WatcherBot.Utils.ANSI;

public enum ForegroundColour
{
    Grey = 30,
    Red = 31,
    Green = 32,
    Yellow = 33,
    Blue = 34,
    Pink = 35,
    Cyan = 36,
    White = 37,
}

public static class ForegroundColourExtensions
{
    public static int GetNumber(this ForegroundColour colour) => (int)colour;

    public static string GetCode(this ForegroundColour colour) => $"\u001b[{colour.GetNumber()}m";
}
