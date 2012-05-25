using System;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Translation.ResultOperatorHandlers;
using Lucene.Net.Linq.Translation.TreeVisitors;
using Lucene.Net.Search;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq.Translation
{
    internal class QueryModelTranslator : QueryModelVisitorBase
    {
        private static readonly ResultOperatorRegistry resultOperators = ResultOperatorRegistry.CreateDefault();

        private readonly Context context;
        private readonly IFieldMappingInfoProvider fieldMappingInfoProvider;
        private readonly LuceneQueryModel model;

        internal QueryModelTranslator(Context context, IFieldMappingInfoProvider fieldMappingInfoProvider)
        {
            this.context = context;
            this.fieldMappingInfoProvider = fieldMappingInfoProvider;
            this.model = new LuceneQueryModel(fieldMappingInfoProvider);
        }

        public void Build(QueryModel queryModel)
        {
            queryModel.Accept(this);
        }

        public LuceneQueryModel Model
        {
            get { return model; }
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            var visitor = new QueryBuildingExpressionTreeVisitor(context, fieldMappingInfoProvider);
            visitor.VisitExpression(whereClause.Predicate);
            
            model.AddQuery(visitor.Query);
        }

        public override void VisitSelectClause(SelectClause selectClause, QueryModel queryModel)
        {
            model.SelectClause = selectClause.Selector;
            model.OutputDataInfo = selectClause.GetOutputDataInfo();
            base.VisitSelectClause(selectClause, queryModel);
        }

        public override void VisitResultOperator(ResultOperatorBase resultOperator, QueryModel queryModel, int index)
        {
            var handler = resultOperators.GetItem(resultOperator.GetType());

            if (handler != null)
            {
                handler.Accept(resultOperator, model);
            }
            else
            {
                model.ApplyUnsupported(resultOperator);
            }

            base.VisitResultOperator(resultOperator, queryModel, index);
        }

        public override void VisitOrderByClause(OrderByClause orderByClause, QueryModel queryModel, int index)
        {
            foreach (var ordering in orderByClause.Orderings)
            {
                model.AddSort(ordering.Expression, ordering.OrderingDirection);
            }
        }
    }
}