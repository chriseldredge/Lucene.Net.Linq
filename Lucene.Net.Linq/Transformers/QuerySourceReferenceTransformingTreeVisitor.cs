using System;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Expressions;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformers
{
    /// <summary>
    /// Replaces MemberExpression instances like [QuerySourceReferenceExpression].PropertyName with <c ref="LuceneQueryFieldExpression"/>
    /// </summary>
    internal class QuerySourceReferenceTransformingTreeVisitor : ExpressionTreeVisitor
    {
        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            if (expression.Expression is QuerySourceReferenceExpression)
            {
                var propertyInfo = expression.Member as PropertyInfo;

                if (propertyInfo == null)
                {
                    throw new NotSupportedException("Only MemberExpression of type PropertyInfo may be used on QuerySourceReferenceExpression.");
                }

                return new LuceneQueryFieldExpression(propertyInfo.PropertyType, propertyInfo.Name);
            }
            
            return base.VisitMemberExpression(expression);
        }
    }
}