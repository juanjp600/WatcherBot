using System;
using System.Collections.Generic;

namespace WatcherBot.Utils;

public class Templates
{
    private readonly Lazy<string> banLazy;
    private readonly Lazy<string> timeoutLazy;

    public Templates()
    {
        banLazy     = new Lazy<string>(() => string.Join('\n', ban));
        timeoutLazy = new Lazy<string>(() => string.Join('\n', timeout));
    }

    public string Ban => banLazy.Value;
    public string Timeout => timeoutLazy.Value;
    public string DefaultAppealRecipient { get; init; } = "";

    public string GetAppealRecipients(string otherName, Anonymous anon) =>
        anon == Anonymous.Yes || otherName == DefaultAppealRecipient
            ? DefaultAppealRecipient
            : $"{otherName} or {DefaultAppealRecipient}";

    // ReSharper disable CollectionNeverUpdated.Local
    private List<string> ban { get; } = new();

    private List<string> timeout { get; } = new();
    // ReSharper restore CollectionNeverUpdated.Local
}
