using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal abstract class MethodInfoMatchingTreeVisitor : ExpressionTreeVisitor
    {
        private readonly HashSet<MethodInfo> methods = new HashSet<MethodInfo>();

        internal void AddMethod(MethodInfo method)
        {
            methods.Add(method.IsGenericMethod ? method.GetGenericMethodDefinition() : method);
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            var method = expression.Method.IsGenericMethod
                             ? expression.Method.GetGenericMethodDefinition()
                             : expression.Method;

            if (!methods.Contains(method)) 
                return base.VisitMethodCallExpression(expression);

            return VisitSupportedMethodCallExpression(expression);
        }

        protected abstract Expression VisitSupportedMethodCallExpression(MethodCallExpression expression);
    }
}