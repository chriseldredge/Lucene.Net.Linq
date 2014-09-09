using System;
using System.Linq;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ResultOperators;
using Remotion.Linq.Transformations;

namespace Lucene.Net.Linq.Transformation.TreeVisitors
{
    internal class AggressiveSubQueryFromClauseFlattener : SubQueryFromClauseFlattener
    {
        protected override void CheckFlattenable(QueryModel subQueryModel)
        {
            var first = subQueryModel.ResultOperators.FirstOrDefault();
            if (first != null)
            {
                throw new NotSupportedException(first.GetType() + " is not supported in sub-queries. Sub-queries may only use SequenceTypePreservingResultOperatorBase subclasses.");
            }
        }

        protected override void FlattenSubQuery(SubQueryExpression subQueryExpression, FromClauseBase fromClause, QueryModel queryModel,
            int destinationIndex)
        {
            var subQueryModel = subQueryExpression.QueryModel;
            MoveResultOperatorsToParent(queryModel, subQueryModel);
            base.FlattenSubQuery(subQueryExpression, fromClause, queryModel, destinationIndex);
        }

        protected virtual void MoveResultOperatorsToParent(QueryModel queryModel, QueryModel subQueryModel)
        {
            foreach (var resultOperator in subQueryModel.ResultOperators.OfType<SequenceTypePreservingResultOperatorBase>().Reverse().ToList())
            {
                queryModel.ResultOperators.Insert(0, resultOperator);
                subQueryModel.ResultOperators.Remove(resultOperator);
            }
        }
    }
}
