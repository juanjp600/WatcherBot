using System.Configuration;
using System.Xml;

namespace WatcherBot.Utils;

public class Range
{
    public int Min { get; init; }
    public int Max { get; init; }

    // public Range(string str)
    // {
    //     string[] split = str.Split(',');
    //     Min = int.Parse(split[0]);
    //     Max = int.Parse(split[1]);
    // }

    public bool Contains(int v) => Min <= v && Max >= v;

    public override string ToString() => $"[{Min}, {Max}]";
}
