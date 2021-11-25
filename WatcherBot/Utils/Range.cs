namespace WatcherBot.Utils
{
    public readonly struct Range
    {
        public readonly int Min;
        public readonly int Max;

        public Range(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public Range(string str)
        {
            string[] split = str.Split(',');
            Min = int.Parse(split[0]);
            Max = int.Parse(split[1]);
        }

        public bool Contains(int v) => Min <= v && Max >= v;

        public override string ToString() => $"[{Min}, {Max}]";
    }
}
