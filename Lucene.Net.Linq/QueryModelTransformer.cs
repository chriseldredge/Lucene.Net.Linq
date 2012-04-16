using System;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Transforms various expressions in a QueryModel instance to make it easier to convert into a Lucene Query.
    /// </summary>
    public class QueryModelTransformer : QueryModelVisitorBase
    {
        private QueryModelTransformer()
        {
        }

        public static void TransformQueryModel(QueryModel queryModel)
        {
            var instance = new QueryModelTransformer();

            var copy = queryModel.Clone();

            queryModel.Accept(instance);

#if DEBUG
            System.Diagnostics.Trace.WriteLine("Pre-transformed QueryModel: " + copy, "Lucene.Net.Linq");
            System.Diagnostics.Trace.WriteLine("Transformed QueryModel:     " + queryModel, "Lucene.Net.Linq");
#endif
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            whereClause.TransformExpressions(new MethodCallExpressionTransformer().VisitExpression);
            
            base.VisitWhereClause(whereClause, queryModel, index);
        }
    }

    internal class MethodCallExpressionTransformer : ExpressionTreeVisitor
    {
        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (expression.Method.Name == "ToLower")
            {
                return expression.Object;
            }

            return base.VisitMethodCallExpression(expression);
        }
    }
}