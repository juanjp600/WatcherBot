using System;
using System.Collections.Generic;

namespace Bot600
{
    public class HashComparer : IEqualityComparer<string>
    {
        public bool Equals(string x, string y)
        {
            if (x is null && y is null)
                return true;
            if (x is null || y is null)
                return false;
            var subLength = Math.Min(x.Length, y.Length);
            var subx = x.Substring(0, subLength);
            var suby = y.Substring(0, subLength);
            return subx == suby;
        }

        public int GetHashCode(string obj)
        {
            return obj.GetHashCode();
        }
    }
}