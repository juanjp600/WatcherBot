using System;

namespace WatcherBot.Config;

public class BanTemplate
{
    private string[] template { get; } = Array.Empty<string>();
    public string Template => string.Join("\n", template);

    public string DefaultAppeal { get; init; } = "";
}
