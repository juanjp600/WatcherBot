using System.Collections.Generic;
using WatcherBot.Utils.ANSI;

namespace WatcherBot.Config;

public class Issues
{
    private HashSet<string> importantIssueLabels { get; } = new();
    public IReadOnlySet<string> ImportantLabels => importantIssueLabels;

    private HashSet<string> ignoreIssueLabels { get; } = new();
    public IReadOnlySet<string> IgnoreLabels => ignoreIssueLabels;

    private Dictionary<string, ForegroundColour> issueLabelColours { get; } = new();
    public IReadOnlyDictionary<string, ForegroundColour> LabelColours => issueLabelColours;
}
