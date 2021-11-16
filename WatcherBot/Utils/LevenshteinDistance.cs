#nullable enable
using System;
using Barotrauma;

namespace WatcherBot.Utils
{
    public static class LevenshteinDistance
    {
        /// <summary>
        /// Calculates the minimum number of single-character edits (i.e. insertions, deletions or substitutions) required to change one string into the other
        /// </summary>
        public static int Calculate(ReadOnlySpan<char> s, ReadOnlySpan<char> t)
        {
            int n = s.Length;
            int m = t.Length;

            if (n == 0 || m == 0) { return Math.Max(n, m); }

            Span<int> d = stackalloc int[(n+1) * (m+1)];

            int calcIndex(int x, int y)
                => y * (n + 1) + x;

            for (int i = 0; i <= n; i++) { d[calcIndex(i, 0)] = i; }
            for (int j = 0; j <= m; j++) { d[calcIndex(0, j)] = j; }

            for (int i = 1; i <= n; i++)
            {
                for (int j = 1; j <= m; j++)
                {
                    int cost = Homoglyphs.Compare(t[j - 1], s[i - 1]) ? 0 : 1;

                    d[calcIndex(i, j)] = Math.Min(
                        Math.Min(d[calcIndex(i - 1, j)] + 1, d[calcIndex(i, j - 1)] + 1),
                        d[calcIndex(i - 1, j - 1)] + cost);
                }
            }

            return d[calcIndex(n, m)];
        }

        public static (int Index, int Length, int Distance)? FindSubstr(string str, string substr, int maxDistance)
        {
            if (str.IndexOf(substr, StringComparison.Ordinal) is int val and > 0) { return (val, substr.Length, 0); }

            int foundIndex = -1;
            int foundLength = -1;
            int foundDistance = -1;
            for (int testLength = substr.Length - maxDistance; testLength <= Math.Min(str.Length, substr.Length + maxDistance); testLength++)
            {
                for (int j = 0; j <= str.Length - testLength; j++)
                {
                    ReadOnlySpan<char> subStrToTest = str.AsSpan(j, testLength);
                    int distance = Calculate(subStrToTest, substr);
                    if (distance <= maxDistance
                        && (foundDistance < 0 || distance < foundDistance))
                    {
                        foundIndex = j;
                        foundLength = testLength;
                        foundDistance = distance;
                    }
                }
            }
            return foundIndex >= 0 ? (foundIndex, foundLength, foundDistance) : null;
        }
    }
}
