using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;

namespace Lucene.Net.Linq
{
    public class LuceneQueryExecutor : IQueryExecutor
    {
        private readonly Directory directory;

        public Document CurrentDocument { get; private set; }

        public LuceneQueryExecutor(Directory directory)
        {
            this.directory = directory;
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
            var mapping = new QuerySourceMapping();
            mapping.AddMapping(queryModel.MainFromClause, Expression.Property(Expression.Constant(this), "CurrentDocument"));

            queryModel.TransformExpressions(e => ReferenceReplacingExpressionTreeVisitor.ReplaceClauseReferences(e, mapping, true));    

            var projection = GetInMemoryProjection<T>(queryModel);
            var projector = projection.Compile();

            using (var searcher = new IndexSearcher(directory, true))
            {
                using (var reader = searcher.GetIndexReader())
                {
                    for (var i = 0; i < reader.MaxDoc(); i++)
                    {
                        if (reader.IsDeleted(i)) continue;

                        CurrentDocument = reader.Document(i);
                        yield return projector(CurrentDocument);
                    }
                }
            }
        }

        public Expression<Func<Document, T>> GetInMemoryProjection<T>(QueryModel queryModel)
        {
            return Expression.Lambda<Func<Document, T>>(queryModel.SelectClause.Selector, Expression.Parameter(typeof(Document)));
        }
    }

}