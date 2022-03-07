namespace WatcherBot.Utils;

public class Templates
{
    public string Ban { get; init; } = "";
    public string Timeout { get; init; } = "";
    public string DefaultAppealRecipient { get; init; } = "";

    public string GetAppealRecipients(string otherName, Anonymous anon) =>
        anon == Anonymous.Yes || otherName == DefaultAppealRecipient
            ? DefaultAppealRecipient
            : $"{otherName} or {DefaultAppealRecipient}";
}
