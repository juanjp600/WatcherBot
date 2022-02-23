using System;

namespace WatcherBot.Config;

public class BanTemplate
{
    private readonly Lazy<string> templateLazy;

    public BanTemplate()
    {
        templateLazy = new Lazy<string>(() => string.Join("\n", template));
    }

    private string[] template { get; } = Array.Empty<string>();
    public string Template => templateLazy.Value;

    public string DefaultAppeal { get; init; } = "";
}
