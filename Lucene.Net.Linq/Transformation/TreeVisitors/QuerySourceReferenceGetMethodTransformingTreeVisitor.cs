using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces MethodCallExpression instances like [QuerySourceReferenceExpression].Get("FieldName") with <c ref="LuceneQueryFieldExpression"/>
    /// </summary>
    internal class QuerySourceReferenceGetMethodTransformingTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Object is QuerySourceReferenceExpression && expression.Method.Name == "Get")
            {
                // TODO: evaluate argument.
                var fieldName = (string)((ConstantExpression)expression.Arguments[0]).Value;
                return new LuceneQueryFieldExpression(typeof(string), fieldName);
            }
            return base.VisitMethodCallExpression(expression);
        }
    }
}