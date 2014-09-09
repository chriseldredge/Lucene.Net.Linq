using System.Collections.Generic;
using Common.Logging;
using Lucene.Net.Linq.Clauses.Expressions;
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
        private static readonly ILog Log = LogManager.GetLogger<QueryModelTransformer>();

        private readonly IEnumerable<ExpressionTreeVisitor> whereSelectClauseVisitors;
        private readonly IEnumerable<ExpressionTreeVisitor> orderingVisitors;

        internal QueryModelTransformer()
            : this(new ExpressionTreeVisitor[]
                       {
                           new SubQueryContainsTreeVisitor(),
                           new LuceneExtensionMethodCallTreeVisitor(),
                           new ExternallyProvidedQueryExpressionTreeVisitor(),
                           new QuerySourceReferencePropertyTransformingTreeVisitor(),
                           new BoostMethodCallTreeVisitor(0),
                           new NoOpMethodCallRemovingTreeVisitor(),
                           new NoOpConditionRemovingTreeVisitor(),
                           new NullSafetyConditionRemovingTreeVisitor(),
                           new NoOpConvertExpressionRemovingVisitor(),
                           new MethodCallToLuceneQueryPredicateExpressionTreeVisitor(),
                           new CompareCallToLuceneQueryPredicateExpressionTreeVisitor(),
                           new FlagToBinaryConditionTreeVisitor(),
                           new BooleanBinaryToQueryPredicateExpressionTreeVisitor(),
                           new BinaryToQueryExpressionTreeVisitor(),
                           new RangeQueryMergeExpressionTreeVisitor(), 
                           new AllowSpecialCharactersMethodExpressionTreeVisitor(),
                           new BoostMethodCallTreeVisitor(1),
                           new FuzzyMethodCallTreeVisitor()
                       },
                   new ExpressionTreeVisitor[]
                       {
                           new LuceneExtensionMethodCallTreeVisitor(),
                           new BoostMethodCallTreeVisitor(1),
                           new QuerySourceReferencePropertyTransformingTreeVisitor(),
                           new NoOpMethodCallRemovingTreeVisitor(),
                           new NullSafetyConditionRemovingTreeVisitor(),
                           new ConcatToCompositeOrderingExpressionTreeVisitor()
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
                whereClause.TransformExpressions(visitor.VisitExpression);
                Log.Trace(m => m("Transformed QueryModel after {0}: {1}", visitor.GetType().Name, queryModel));
            }

            base.VisitWhereClause(whereClause, queryModel, index);
        }

        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        {
            Log.Trace(m => m("Original QueryModel:     {0}", queryModel));

            foreach (var visitor in orderingVisitors)
            {
                orderByClause.TransformExpressions(visitor.VisitExpression);
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
