using System.Linq;
using Lucene.Net.Linq.Clauses;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Translation.ExpressionVisitors;
using Lucene.Net.Linq.Translation.ResultOperatorHandlers;
using Lucene.Net.Search;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq.Translation
{
    internal class QueryModelTranslator : QueryModelVisitorBase, ILuceneQueryModelVisitor
    {
        private static readonly ResultOperatorRegistry resultOperators = ResultOperatorRegistry.CreateDefault();

        private readonly IFieldMappingInfoProvider fieldMappingInfoProvider;
        private readonly Context context;
        private readonly LuceneQueryModel model;

        internal QueryModelTranslator(IFieldMappingInfoProvider fieldMappingInfoProvider, Context context)
        {
            this.fieldMappingInfoProvider = fieldMappingInfoProvider;
            this.context = context;
            this.model = new LuceneQueryModel(fieldMappingInfoProvider);
        }

        public void Build(QueryModel queryModel)
        {
            queryModel.Accept(this);

            if (context.Settings.EnableMultipleEntities)
            {
                CreateQueryFilterForKeyFields();
            }
        }

        public LuceneQueryModel Model
        {
            get { return model; }
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            var visitor = new QueryBuildingExpressionVisitor(fieldMappingInfoProvider);
            visitor.Visit(whereClause.Predicate);

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

        public void VisitBoostClause(BoostClause clause, QueryModel queryModel, int index)
        {
            model.AddBoostFunction(clause.BoostFunction);
        }

        public void VisitTrackRetrievedDocumentsClause(TrackRetrievedDocumentsClause clause, QueryModel queryModel, int index)
        {
            model.DocumentTracker = clause.Tracker.Value;
        }

        public void VisitQueryStatisticsCallbackClause(QueryStatisticsCallbackClause clause, QueryModel queryModel, int index)
        {
            model.AddQueryStatisticsCallback(clause.Callback);
        }

        private void CreateQueryFilterForKeyFields()
        {
            var filterQuery = fieldMappingInfoProvider.KeyProperties.Aggregate(
                new BooleanQuery(),
                (query, property) =>
                    {
                        var fieldMappingInfo = fieldMappingInfoProvider.GetMappingInfo(property);

                        query.Add(fieldMappingInfo.CreateQuery("*"), Occur.MUST);
                        return query;
                    });

            if (filterQuery.Clauses.Any())
            {
                model.Filter = new QueryWrapperFilter(filterQuery);
            }
        }
    }
}
