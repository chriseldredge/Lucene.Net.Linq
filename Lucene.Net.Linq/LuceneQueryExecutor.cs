using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Transformation;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;

namespace Lucene.Net.Linq
{
    internal class QueryExecutor<TDocument> : LuceneQueryExecutor<TDocument>
    {
        private readonly Func<TDocument> newItem;
        private readonly IDocumentMapper<TDocument> mapper;

        public QueryExecutor(Directory directory, Context context, Func<TDocument> newItem, IDocumentMapper<TDocument> mapper)
            : base(directory, context)
        {
            this.newItem = newItem;
            this.mapper = mapper;
        }

        protected override void SetCurrentDocument(Document doc)
        {
            var item = newItem();

            mapper.ToObject(doc, item);

            CurrentDocument = item;
        }

        public override IFieldMappingInfo GetMappingInfo(string fieldName)
        {
            return mapper.GetMappingInfo(fieldName);
        }
    }

    internal abstract class LuceneQueryExecutor<TDocument> : IQueryExecutor, IFieldMappingInfoProvider
    {
        private readonly Directory directory;
        private readonly Context context;

        public TDocument CurrentDocument { get; protected set; }

        protected LuceneQueryExecutor(Directory directory, Context context)
        {
            this.directory = directory;
            this.context = context;
        }

        public T ExecuteScalar<T>(QueryModel queryModel)
        {
            return default(T);
        }

        public T ExecuteSingle<T>(QueryModel queryModel, bool returnDefaultWhenEmpty)
        {
            var sequence = ExecuteCollection<T>(queryModel);

            return returnDefaultWhenEmpty ? sequence.SingleOrDefault() : sequence.Single();
        }

        public IEnumerable<T> ExecuteCollection<T>(QueryModel queryModel)
        {
            QueryModelTransformer.TransformQueryModel(queryModel);

            var builder = new QueryModelTranslator(context, this);
            builder.Build(queryModel);

#if DEBUG
            System.Diagnostics.Trace.WriteLine("Lucene query: " + builder.Query + " sort: " + builder.Sort, "Lucene.Net.Linq");
#endif

            var mapping = new QuerySourceMapping();
            mapping.AddMapping(queryModel.MainFromClause, GetCurrentRowExpression());
            queryModel.TransformExpressions(e => ReferenceReplacingExpressionTreeVisitor.ReplaceClauseReferences(e, mapping, true));

            var projection = GetProjector<T>(queryModel);
            var projector = projection.Compile();
            
            using (var searcher = new IndexSearcher(directory, true))
            {
                var skipResults = builder.SkipResults;
                var maxResults = Math.Min(builder.MaxResults, searcher.MaxDoc() - skipResults);
                
                var hits = searcher.Search(builder.Query, null, maxResults + skipResults, builder.Sort);

                for (var i = skipResults; i < hits.ScoreDocs.Length; i++)
                {
                    SetCurrentDocument(searcher.Doc(hits.ScoreDocs[i].Doc));
                    yield return projector(CurrentDocument);
                }
            }
        }

        public abstract IFieldMappingInfo GetMappingInfo(string fieldName);

        protected abstract void SetCurrentDocument(Document doc);

        protected virtual Expression GetCurrentRowExpression()
        {
            return Expression.Property(Expression.Constant(this), "CurrentDocument");
        }

        protected virtual Expression<Func<TDocument, T>> GetProjector<T>(QueryModel queryModel)
        {
            return Expression.Lambda<Func<TDocument, T>>(queryModel.SelectClause.Selector, Expression.Parameter(typeof(TDocument)));
        }
    }
}