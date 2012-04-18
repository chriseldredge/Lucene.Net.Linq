using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    /// <summary>
    /// Replaces MemberExpression instances like [QuerySourceReferenceExpression].PropertyName with <c ref="LuceneQueryFieldExpression"/>
    /// </summary>
    internal class QuerySourceReferencePropertyTransformingTreeVisitor : ExpressionTreeVisitor
    {
        private MemberExpression parent;
        private LuceneQueryFieldExpression result;

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            parent = expression;
            
            var x = base.VisitMemberExpression(expression);

            return result ?? x;
        }

        protected override Expression VisitQuerySourceReferenceExpression(QuerySourceReferenceExpression expression)
        {
            var propertyInfo = parent.Member as PropertyInfo;

            if (propertyInfo == null)
            {
                throw new NotSupportedException("Only MemberExpression of type PropertyInfo may be used on QuerySourceReferenceExpression.");
            }

            result = new LuceneQueryFieldExpression(propertyInfo.PropertyType, propertyInfo.Name);
            return base.VisitQuerySourceReferenceExpression(expression);
        }
    }
}