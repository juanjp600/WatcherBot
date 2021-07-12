using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Bot600.Utils
{
    public static class Reflection
    {
        public static void CopyFields<TSuper, TSub>(this TSub subclass, TSuper superclass) where TSub : TSuper
        {
            IEnumerable<FieldInfo?> x =
                typeof(TSuper)
                    .FindMembers(MemberTypes.Field, BindingFlags.Public | BindingFlags.Instance, null, null)
                    .Select(mi => mi as FieldInfo);

            foreach (FieldInfo? fi in x)
            {
                fi?.SetValue(subclass, fi.GetValue(superclass));
            }
        }
    }
}
