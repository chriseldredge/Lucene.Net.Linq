using System.Linq;
using System.Linq.Expressions;
using Remotion.Linq;
using Remotion.Linq.Parsing.Structure;

namespace Lucene.Net.Linq
{
    public class LuceneQueryable<T> : QueryableBase<T>
    {
        public LuceneQueryable(IQueryParser queryParser, IQueryExecutor executor)
            : base(new DefaultQueryProvider(typeof(LuceneQueryable<>), queryParser, executor))
        {
        }

        public LuceneQueryable(IQueryProvider provider, Expression expression)
            : base(provider, expression)
        {
        }
    }
}