using System;

namespace WatcherBot.Utils.ANSI;

public enum Style
{
    Normal,
    Bold,
    Underline,
}

public static class StyleExtensions
{
    public static string GetCode(this Style style) =>
        style switch {
            Style.Normal    => "\u001b[0m",
            Style.Bold      => "\u001b[1m",
            Style.Underline => "\u001b[4m",
            var _           => throw new ArgumentOutOfRangeException(nameof(style), style, null),
        };
}
