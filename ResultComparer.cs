using System;
using System.Collections.Generic;

namespace Bot600
{
    public class HashComparer : IEqualityComparer<Result<string>>
    {
        public bool Equals(Result<string> x, Result<string> y)
        {
            if (x is null && y is null) { return true; }
            if (x is null || y is null) { return false; }
            if (x.IsSuccess != y.IsSuccess) { return false; }
            if (!x.IsSuccess) { return x.FailureMessage == y.FailureMessage; }
            return x.Value.StartsWith(y.Value, StringComparison.OrdinalIgnoreCase)
                || y.Value.StartsWith(x.Value, StringComparison.OrdinalIgnoreCase);
        }

        public int GetHashCode(Result<string> obj)
        {
            var baseHash = obj.IsSuccess ? obj.Value.Substring(0, 5).GetHashCode() : obj.FailureMessage.GetHashCode();
            var lsb = obj.IsSuccess ? 1 : 0;
            return (baseHash << 1) | lsb;
        }
    }
}
