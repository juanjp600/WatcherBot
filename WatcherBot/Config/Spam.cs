using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
// ReSharper disable CollectionNeverUpdated.Local

namespace WatcherBot.Config;

public class Spam
{
    private readonly Lazy<IReadOnlySet<(string Substring, int MaxDistance, float Weight)>> spam;

    public Spam()
    {
        spam = new Lazy<IReadOnlySet<(string Substring, int MaxDistance, float Weight)>>(() =>
            spamSubstrings.Zip(spamSubstringMaxDist)
                          .Zip(spamSubstringWeights, (tup, dist) => (tup.First, tup.Second, fl: dist))
                          .ToImmutableHashSet());
    }

    private List<string> spamSubstrings { get; } = new();
    private List<float> spamSubstringWeights { get; } = new();
    private List<int> spamSubstringMaxDist { get; } = new();
    public IReadOnlySet<(string Substring, int MaxDistance, float Weight)> SpamSubstrings => spam.Value;
}
