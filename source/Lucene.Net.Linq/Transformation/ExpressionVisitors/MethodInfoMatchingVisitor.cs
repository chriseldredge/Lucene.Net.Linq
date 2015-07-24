using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.ExpressionVisitors
{
    internal abstract class MethodInfoMatchingVisitor : RelinqExpressionVisitor
    {
        private readonly HashSet<MethodInfo> methods = new HashSet<MethodInfo>();

        internal void AddMethod(MethodInfo method)
        {
            methods.Add(method.IsGenericMethod ? method.GetGenericMethodDefinition() : method);
        }

        protected override Expression VisitMethodCall(MethodCallExpression expression)
        {
            var method = expression.Method.IsGenericMethod
                             ? expression.Method.GetGenericMethodDefinition()
                             : expression.Method;

            if (!methods.Contains(method))
                return base.VisitMethodCall(expression);

            return VisitSupportedMethodCall(expression);
        }

        protected abstract Expression VisitSupportedMethodCall(MethodCallExpression expression);
    }
}
