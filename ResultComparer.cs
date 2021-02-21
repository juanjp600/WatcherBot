using System;
using System.Collections.Generic;

namespace Bot600
{
    public class ResultComparer<T> : IEqualityComparer<Result<T>>
    {
        public bool Equals(Result<T> x, Result<T> y) => x == y;

        public int GetHashCode(Result<T> obj) => obj.GetHashCode();
    }
}
