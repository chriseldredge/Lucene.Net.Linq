using System.Linq;
using System.Reflection;

namespace Lucene.Net.Linq.Util
{
    public static class MemberInfoUtils
    {
        public static T GetCustomAttribute<T>(this MemberInfo member, bool inherit)
        {
            return (T) member.GetCustomAttributes(typeof (T), inherit).SingleOrDefault();
        }
    }
}