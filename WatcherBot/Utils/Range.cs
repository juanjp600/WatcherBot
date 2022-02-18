namespace WatcherBot.Utils;

public class Range
{
    public int Min { get; init; }
    public int Max { get; init; }

    public bool Contains(int v) => Min <= v && Max >= v;

    public override string ToString() => $"[{Min}, {Max}]";
}
