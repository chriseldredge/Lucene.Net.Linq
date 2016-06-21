using System.Collections.Generic;
using System.Linq.Expressions;
using Common.Logging;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Transformation.ExpressionVisitors;
using Lucene.Net.Linq.Util;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq.Transformation
{
    /// <summary>
    /// Transforms various expressions in a QueryModel instance to make it easier to convert into a Lucene Query.
    /// </summary>
    internal class QueryModelTransformer : QueryModelVisitorBase
    {
        private static readonly ILog Log = LogManager.GetLogger<QueryModelTransformer>();

        private readonly IEnumerable<ExpressionVisitor> whereSelectClauseVisitors;
        private readonly IEnumerable<ExpressionVisitor> orderingVisitors;

        internal QueryModelTransformer()
            : this(new ExpressionVisitor[]
                       {
                           new SubQueryContainsVisitor(),
                           new LuceneExtensionMethodCallVisitor(),
                           new ExternallyProvidedQueryExpressionVisitor(),
                           new QuerySourceReferencePropertyTransformingVisitor(),
                           new BoostMethodCallVisitor(0),
                           new NoOpMethodCallRemovingVisitor(),
                           new NoOpConditionRemovingVisitor(),
                           new NullSafetyConditionRemovingVisitor(),
                           new NoOpConvertExpressionRemovingVisitor(),
                           new MethodCallToLuceneQueryPredicateExpressionVisitor(),
                           new CompareCallToLuceneQueryPredicateExpressionVisitor(),
                           new FlagToBinaryConditionVisitor(),
                           new BooleanBinaryToQueryPredicateExpressionVisitor(),
                           new BinaryToQueryExpressionVisitor(),
                           new RangeQueryMergeExpressionVisitor(),
                           new AllowSpecialCharactersMethodExpressionVisitor(),
                           new BoostMethodCallVisitor(1),
                           new FuzzyMethodCallVisitor()
                       },
                   new ExpressionVisitor[]
                       {
                           new LuceneExtensionMethodCallVisitor(),
                           new BoostMethodCallVisitor(1),
                           new QuerySourceReferencePropertyTransformingVisitor(),
                           new NoOpMethodCallRemovingVisitor(),
                           new NullSafetyConditionRemovingVisitor(),
                           new ConcatToCompositeOrderingExpressionVisitor()
                       })
        {
        }

        internal QueryModelTransformer(IEnumerable<ExpressionVisitor> whereSelectClauseVisitors, IEnumerable<ExpressionVisitor> orderingVisitors)
        {
            this.whereSelectClauseVisitors = whereSelectClauseVisitors;
            this.orderingVisitors = orderingVisitors;
        }

        public static void TransformQueryModel(QueryModel queryModel)
        {
            var instance = new QueryModelTransformer();

            queryModel.Accept(instance);
        }

        public override void VisitMainFromClause(MainFromClause fromClause, QueryModel queryModel)
        {
            Log.Trace(m => m("Original QueryModel:     {0}", queryModel));
            new AggressiveSubQueryFromClauseFlattener().VisitMainFromClause(fromClause, queryModel);
            Log.Trace(m => m("Transformed QueryModel after AggressiveSubQueryFromClauseFlattener: {0}", queryModel));
            base.VisitMainFromClause(fromClause, queryModel);
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            Log.Trace(m => m("Original QueryModel:     {0}", queryModel));

            foreach (var visitor in whereSelectClauseVisitors)
            {
                whereClause.TransformExpressions(visitor.Visit);
                Log.Trace(m => m("Transformed QueryModel after {0}: {1}", visitor.GetType().Name, queryModel));
            }

            base.VisitWhereClause(whereClause, queryModel, index);
        }

        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        {
            Log.Trace(m => m("Original QueryModel:     {0}", queryModel));

            foreach (var visitor in orderingVisitors)
            {
                orderByClause.TransformExpressions(visitor.Visit);
                Log.Trace(m => m("Transformed QueryModel after {0}: {1}", visitor.GetType().Name, queryModel));
            }

            ExpandCompositeOrderings(orderByClause);

            base.VisitOrderByClause(orderByClause, queryModel, index);
        }

        private void ExpandCompositeOrderings(OrderByClause orderByClause)
        {
            var orderings = orderByClause.Orderings;
            var copy = new Ordering[orderings.Count];
            orderings.CopyTo(copy, 0);

            copy.Apply(o => orderings.Remove(o));

            foreach (var o in copy)
            {
                if (o.Expression is LuceneCompositeOrderingExpression)
                {
                    var ex = (LuceneCompositeOrderingExpression) o.Expression;

                    ex.Fields.Apply(f => orderings.Add(new Ordering(f, o.OrderingDirection)));
                }
                else
                {
                    orderings.Add(o);
                }
            }
        }
    }
}
