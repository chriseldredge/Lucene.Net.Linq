using System;
using System.Linq.Expressions;
using System.Reflection;

namespace Lucene.Net.Linq.Util
{
    public static class MemberInfoUtils
    {
        public static MethodInfo GetMethod<T>(Expression<Func<T>> wrappedCall)
        {
            MethodInfo method = null;

            switch (wrappedCall.Body.NodeType)
            {
                case ExpressionType.Call:
                    method = ((MethodCallExpression)wrappedCall.Body).Method;
                    break;
                case ExpressionType.MemberAccess:
                    var memberExpression = (MemberExpression)wrappedCall.Body;
                    var property = memberExpression.Member as PropertyInfo;
                    method = property != null ? property.GetGetMethod() : null;
                    break;
            }

            if (method == null)
            {
                throw new ArgumentException(string.Format("Cannot extract a method from the given expression '{0}'.", wrappedCall.Body), "wrappedCall");
            }

            return method;
        }

        public static MethodInfo GetGenericMethod<T>(Expression<Func<T>> wrappedCall)
        {
            var method = GetMethod(wrappedCall);

            if (method.IsGenericMethod)
            {
                method = method.GetGenericMethodDefinition();
            }

            return method;
        }
    }
}
