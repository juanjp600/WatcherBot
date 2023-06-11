using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

// ReSharper disable CollectionNeverUpdated.Local

namespace WatcherBot.Config;

public class BadWords
{
    private readonly Lazy<IReadOnlySet<(string Substring, int MaxDistance)>> spam;

    public BadWords()
    {
        spam = new Lazy<IReadOnlySet<(string Substring, int MaxDistance)>>(() =>
            badSubstrings.Zip(badSubstringMaxDist)
                          .ToImmutableHashSet());
    }

    private List<string> badSubstrings { get; } = new();
    private List<int> badSubstringMaxDist { get; } = new();
    public IReadOnlySet<(string Substring, int MaxDistance)> BadSubstrings => spam.Value;
}
