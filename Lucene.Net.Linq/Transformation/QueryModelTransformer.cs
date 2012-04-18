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
        private readonly IEnumerable<ExpressionTreeVisitor> visitors;

        internal QueryModelTransformer()
            : this(new ExpressionTreeVisitor[]
                       {
                           new QuerySourceReferenceGetMethodTransformingTreeVisitor(),
                           new QuerySourceReferencePropertyTransformingTreeVisitor(),
                           new FlagToBinaryConditionTreeVisitor(),
                           new NoOpMethodCallRemovingTreeVisitor(),
                           new MethodCallToBinaryExpressionTreeVisitor(),
                           new NullSafetyConditionRemovingTreeVisitor()
                       })
        {
        }

        internal QueryModelTransformer(IEnumerable<ExpressionTreeVisitor> visitors)
        {
            this.visitors = visitors;
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
            ApplyVisitors(whereClause.TransformExpressions);

            base.VisitWhereClause(whereClause, queryModel, index);
        }

        public override void VisitOrdering(Ordering ordering, QueryModel queryModel, OrderByClause orderByClause, int index)
        {
            ApplyVisitors(ordering.TransformExpressions);

            base.VisitOrdering(ordering, queryModel, orderByClause, index);
        }

        private void ApplyVisitors(Action<Func<Expression, Expression>> action)
        {
            visitors.Apply(v => action(v.VisitExpression));
        }
    }
}