using System.Collections.Generic;
using WatcherBot.Utils.ANSI;

namespace WatcherBot.Config;

public class Issues
{
    private Dictionary<string, int> labelWeighting { get; } = new();
    public IReadOnlyDictionary<string, int> LabelWeighting => labelWeighting;

    private HashSet<string> hideLabels { get; } = new();
    public IReadOnlySet<string> HideLabels => hideLabels;

    private Dictionary<string, ForegroundColour> labelColours { get; } = new();
    public IReadOnlyDictionary<string, ForegroundColour> LabelColours => labelColours;
}
