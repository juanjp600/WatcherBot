using Microsoft.Extensions.Configuration;
using System;

namespace WatcherBot.Utils;

public class Templates
{
    public string Ban { get; }
    public string Timeout { get; }

    private string DefaultAppealRecipent;

    private Templates(string ban, string timeout, string defaultAppealRecipent) {
        Ban = ban;
        Timeout = timeout;
        DefaultAppealRecipent = defaultAppealRecipent;
    }

    public string GetAppealRecipients(string otherName, Anonymous anon) {
        if (anon == Anonymous.Yes || otherName == DefaultAppealRecipent) {
            return DefaultAppealRecipent;
        } else {
            return $"{otherName} or {DefaultAppealRecipent}";
        }
    }

    public static Templates FromConfig(Config.Config config)
    {
        var section = config.Configuration.GetSection("Templates");
        Func<string, string> arrSecToStr = (key) => string.Join("\n", section.GetSection(key).Get<string[]>());
        return new Templates(arrSecToStr("Ban"), arrSecToStr("Timeout"), section.GetSection("DefaultAppeal").Get<string>());
    }
}
