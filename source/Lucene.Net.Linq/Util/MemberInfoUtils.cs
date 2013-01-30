using System.Linq;
using System.Reflection;

namespace Lucene.Net.Linq.Util
{
    public static class MemberInfoUtils
    {
        public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit)
        {
            return member.GetCustomAttributes<T>(inherit).SingleOrDefault();
        }

        public static T[] GetCustomAttributes<T>(this MemberInfo member, bool inherit)
        {
            return member.GetCustomAttributes(typeof (T), inherit).Cast<T>().ToArray();
        }
    }
}