using System;
using System.Collections.Generic;

namespace Bot600.Monads
{
    public class HashComparer : IEqualityComparer<Result<string, string>>
    {
        public bool Equals(Result<string, string>? x, Result<string, string>? y)
        {
            if (x is null && y is null)
            {
                return true;
            }

            if (x is null || y is null)
            {
                return false;
            }

            return (x, y) switch
                   {
                       (Ok<string, string> xok, Ok<string, string> yok) =>
                           xok.Value.StartsWith(yok.Value, StringComparison.OrdinalIgnoreCase) ||
                           yok.Value.StartsWith(xok.Value, StringComparison.OrdinalIgnoreCase),
                       (Error<string, string> xerr, Error<string, string> yerr) => xerr.Value == yerr.Value,
                       _                                                        => false
                   };
        }

        public int GetHashCode(Result<string, string> obj)
        {
            (int baseHash, int lsb) = obj switch
                                      {
                                          Ok<string, string> ok     => (ok.Value[..5].GetHashCode(), 1),
                                          Error<string, string> err => (err.Value.GetHashCode(), 0),
                                          _                         => throw new ArgumentOutOfRangeException(nameof(obj)),
                                      };
            return (baseHash << 1) | lsb;
        }
    }
}
