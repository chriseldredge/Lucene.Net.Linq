using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Lucene.Net.Linq.Transformation.TreeVisitors;
using Lucene.Net.Linq.Util;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Parsing;

namespace Lucene.Net.Linq.Transformation
{
    /// <summary>
    /// Transforms various expressions in a QueryModel instance to make it easier to convert into a Lucene Query.
    /// </summary>
    internal class QueryModelTransformer : QueryModelVisitorBase
    {
        private readonly IEnumerable<ExpressionTreeVisitor> whereSelectClauseVisitors;
        private readonly IEnumerable<ExpressionTreeVisitor> orderingVisitors;

        internal QueryModelTransformer()
            : this(new ExpressionTreeVisitor[]
                       {
                           new QuerySourceReferenceGetMethodTransformingTreeVisitor(),
                           new QuerySourceReferencePropertyTransformingTreeVisitor(),
                           new FlagToBinaryConditionTreeVisitor(),
                           new NoOpMethodCallRemovingTreeVisitor(),
                           new MethodCallToBinaryExpressionTreeVisitor(),
                           new NullSafetyConditionRemovingTreeVisitor(),
                           // TODO: new EvaluateToContantTransformer()
                           new BinaryToQueryExpressionTreeVisitor()
                       },
                   new ExpressionTreeVisitor[]
                       {
                           new QuerySourceReferenceGetMethodTransformingTreeVisitor(),
                           new QuerySourceReferencePropertyTransformingTreeVisitor(),
                           new NoOpMethodCallRemovingTreeVisitor()
                       })
        {
        }

        internal QueryModelTransformer(IEnumerable<ExpressionTreeVisitor> whereSelectClauseVisitors, IEnumerable<ExpressionTreeVisitor> orderingVisitors)
        {
            this.whereSelectClauseVisitors = whereSelectClauseVisitors;
            this.orderingVisitors = orderingVisitors;
        }

        public static void TransformQueryModel(QueryModel queryModel)
        {
            var instance = new QueryModelTransformer();

#if DEBUG
            System.Diagnostics.Trace.WriteLine("Original QueryModel:     " + queryModel, "Lucene.Net.Linq");
#endif
            queryModel.Accept(instance);

#if DEBUG
            System.Diagnostics.Trace.WriteLine("Transformed QueryModel:  " + queryModel, "Lucene.Net.Linq");
#endif
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            whereSelectClauseVisitors.Apply(v => ((Action<Func<Expression, Expression>>) whereClause.TransformExpressions)(v.VisitExpression));

            base.VisitWhereClause(whereClause, queryModel, index);
        }

        public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index)
        {
            orderingVisitors.Apply(v => ((Action<Func<Expression, Expression>>)ordering.TransformExpressions)(v.VisitExpression));

            base.VisitOrdering(ordering, queryModel, orderByClause, index);
        }
    }
}