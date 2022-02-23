using Microsoft.Extensions.Configuration;
using System;

namespace WatcherBot.Utils;

public record Templates(string Ban, string Timeout, string DefaultAppealRecipient)
{
    public string GetAppealRecipients(string otherName, Anonymous anon)
        => anon == Anonymous.Yes || otherName == DefaultAppealRecipient
            ? DefaultAppealRecipient
            : $"{otherName} or {DefaultAppealRecipient}";
}
