using System;
using System.Collections.Generic;

namespace WatcherBot.Utils;

public class Templates
{
    private string? banLazy;
    private string? timeoutLazy;

    public string Ban
    {
        get
        {
            if (ban.Count == 0) { return ""; }
            return banLazy ??= string.Join("\n", ban);
        }
    }

    public string Timeout
    {
        get
        {
            if (timeout.Count == 0) { return ""; }
            return timeoutLazy ??= string.Join("\n", timeout);
        }
    }

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
