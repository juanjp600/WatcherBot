using System.Collections.Generic;
using WatcherBot.Utils.ANSI;

namespace WatcherBot.Config;

public class Issues
{
    private HashSet<string> importantLabels { get; } = new();
    public IReadOnlySet<string> ImportantLabels => importantLabels;

    private HashSet<string> ignoreLabels { get; } = new();
    public IReadOnlySet<string> IgnoreLabels => ignoreLabels;

    private Dictionary<string, ForegroundColour> labelColours { get; } = new();
    public IReadOnlyDictionary<string, ForegroundColour> LabelColours => labelColours;
}
