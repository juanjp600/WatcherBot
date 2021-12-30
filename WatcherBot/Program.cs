namespace WatcherBot;

internal static class Program
{
    public static void Main(string[] args)
    {
        using var bm = new BotMain();
        bm.MainAsync().GetAwaiter().GetResult();
    }
}
