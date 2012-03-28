using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Documents;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Remotion.Linq;

namespace Lucene.Net.Linq
{
    public class LuceneQueryExecutor : IQueryExecutor
    {
        private readonly Directory directory;
        private Document current;

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
            var projection = GetInMemoryProjection<T>(queryModel, () => current);
            var projector = projection.Compile();

            var searcher = new IndexSearcher(directory, true);

            using (var reader = searcher.GetIndexReader())
            {
                for (var i = 0; i < reader.MaxDoc(); i++)
                {
                    if (reader.IsDeleted(i)) continue;

                    current = reader.Document(i);
                    return new[] { projector(current) };
                }
            }

            return null;
        }

        public Expression<Func<Document, T>> GetInMemoryProjection<T>(QueryModel queryModel, Expression<Func<Document>> current)
        {
            var t = new QueryTransformer(current);
            queryModel.TransformExpressions(t.Replace);
            return Expression.Lambda<Func<Document, T>>(queryModel.SelectClause.Selector, Expression.Parameter(typeof(Document)));
        }
    }
}