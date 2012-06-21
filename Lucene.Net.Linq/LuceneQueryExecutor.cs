using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search.Function;
using Lucene.Net.Linq.Transformation;
using Lucene.Net.Linq.Translation;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;
using Remotion.Linq.Clauses.ResultOperators;

namespace Lucene.Net.Linq
{
    internal class LuceneQueryExecutor<TDocument> : LuceneQueryExecutorBase<TDocument>
    {
        private readonly Func<TDocument> newItem;
        private readonly IDocumentMapper<TDocument> mapper;

        public LuceneQueryExecutor(Context context, Func<TDocument> newItem, IDocumentMapper<TDocument> mapper)
            : base(context)
        {
            this.newItem = newItem;
            this.mapper = mapper;
        }

        protected override TDocument ConvertDocument(Document doc, float score)
        {
            var item = newItem();

            mapper.ToObject(doc, score, item);
            
            return item;
        }

        public override IFieldMappingInfo GetMappingInfo(string propertyName)
        {
            return mapper.GetMappingInfo(propertyName);
        }

        public override IEnumerable<string> AllFields
        {
            get { return mapper.AllFields; }
        }

        protected override bool EnableScoreTracking
        {
            get { return mapper.EnableScoreTracking; }
        }
    }

    internal abstract class LuceneQueryExecutorBase<TDocument> : IQueryExecutor, IFieldMappingInfoProvider
    {
        private readonly Context context;
        
        protected LuceneQueryExecutorBase(Context context)
        {
            this.context = context;
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            var luceneQueryModel = PrepareQuery(queryModel);

            var searcherHandle = context.CheckoutSearcher(this);

            using (searcherHandle)
            {
                var searcher = searcherHandle.Searcher;
                var skipResults = luceneQueryModel.SkipResults;
                var maxResults = Math.Min(luceneQueryModel.MaxResults, searcher.MaxDoc() - skipResults);

                var hits = searcher.Search(luceneQueryModel.Query, null, maxResults, luceneQueryModel.Sort);

                var projection = GetScalarProjector<T>(luceneQueryModel.ResultSetOperator, hits);
                var projector = projection.Compile();

                return projector(hits);
            }
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var sequence = ExecuteCollection<T>(queryModel);

            return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
        }

        public class ItemHolder
        {
            public TDocument Current { get; set; }
        }

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            var itemHolder = new ItemHolder();

            var currentItemExpression = Expression.Property(Expression.Constant(itemHolder), "Current");

            var luceneQueryModel = PrepareQuery(queryModel);

            // TODO: move this into QueryModelTransformer?
            var mapping = new QuerySourceMapping();
            mapping.AddMapping(queryModel.MainFromClause, currentItemExpression);
            queryModel.TransformExpressions(e => ReferenceReplacingExpressionTreeVisitor.ReplaceClauseReferences(e, mapping, true));

            var projection = GetProjector<T>(queryModel);
            var projector = projection.Compile();

            var searcherHandle = context.CheckoutSearcher(this);

            using (searcherHandle)
            {
                var searcher = searcherHandle.Searcher;
                var skipResults = luceneQueryModel.SkipResults;
                var maxResults = Math.Min(luceneQueryModel.MaxResults, searcher.MaxDoc() - skipResults);
                var query = luceneQueryModel.Query;

                var scoreFunction = luceneQueryModel.GetCustomScoreFunction<TDocument>();
                if (scoreFunction != null)
                {
                    query = new DelegatingCustomScoreQuery<TDocument>(query, ConvertDocument, scoreFunction);
                }

                if (EnableScoreTracking)
                {
                    searcher.SetDefaultFieldSortScoring(true, false);
                }

                var hits = searcher.Search(query, null, maxResults + skipResults, luceneQueryModel.Sort);
                
                if (luceneQueryModel.Last)
                {
                    skipResults = hits.ScoreDocs.Length - 1;
                    if (skipResults < 0) yield break;
                }

                for (var i = skipResults; i < hits.ScoreDocs.Length; i++)
                {
                    itemHolder.Current = ConvertDocument(searcher.Doc(hits.ScoreDocs[i].doc), hits.ScoreDocs[i].score);
                    yield return projector(itemHolder.Current);
                }
            }
        }

        private LuceneQueryModel PrepareQuery(QueryModel queryModel)
        {
            QueryModelTransformer.TransformQueryModel(queryModel);

            var builder = new QueryModelTranslator(context, this);
            builder.Build(queryModel);

            Log.Trace(() => "Lucene query: " + builder.Model);

            return builder.Model;
        }

        public abstract IFieldMappingInfo GetMappingInfo(string propertyName);
        public abstract IEnumerable<string> AllFields { get; }

        protected abstract TDocument ConvertDocument(Document doc, float score);
        protected abstract bool EnableScoreTracking { get; }

        protected virtual Expression<Func<TDocument, T>> GetProjector<T>(QueryModel queryModel)
        {
            return Expression.Lambda<Func<TDocument, T>>(queryModel.SelectClause.Selector, Expression.Parameter(typeof(TDocument)));
        }

        protected virtual Expression<Func<TopFieldDocs, T>> GetScalarProjector<T>(ResultOperatorBase op, TopFieldDocs docs)
        {
            Expression call = Expression.Call(Expression.Constant(this), GetType().GetMethod("DoCount"), Expression.Constant(docs));
            if (op is LongCountResultOperator)
            {
                call = Expression.Convert(call, typeof(long));
            }
            else if (op is AnyResultOperator)
            {
                call = Expression.Call(Expression.Constant(this), GetType().GetMethod("DoAny"), Expression.Constant(docs));
            }
            else if (!(op is CountResultOperator))
            {
                //TODO: resultOperator.ExecuteInMemory() on unsupported ones.
                throw new NotSupportedException("The result operator type " + op.GetType() + " is not supported.");
            }
            
            return Expression.Lambda<Func<TopFieldDocs, T>>(call, Expression.Parameter(typeof(TopFieldDocs)));
        }

        public bool DoAny(TopFieldDocs d)
        {
            return d.TotalHits != 0;
        }

        public int DoCount(TopFieldDocs d)
        {
            return d.ScoreDocs.Length;
        }
    }
}