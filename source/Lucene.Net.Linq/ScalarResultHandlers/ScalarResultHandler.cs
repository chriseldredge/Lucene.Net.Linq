using System;
using System.Collections.Generic;
using Lucene.Net.Search;
using Remotion.Linq.Clauses.ResultOperators;

namespace Lucene.Net.Linq.ScalarResultHandlers
{
    internal abstract class ScalarResultHandler
    {
        public abstract IEnumerable<Type> SupportedTypes { get; }

        public T Execute<T>(TopFieldDocs hits)
        {
            return (T) Convert.ChangeType(Execute(hits), typeof (T));
        }

        protected abstract object Execute(TopFieldDocs hits);
    }

    internal class CountResultHandler : ScalarResultHandler
    {
        public override IEnumerable<Type> SupportedTypes
        {
            get { return new[] {typeof (CountResultOperator), typeof (LongCountResultOperator)}; }
        }

        protected override object Execute(TopFieldDocs hits)
        {
            return hits.ScoreDocs.Length;
        }
    }

    internal class AnyResultHandler : ScalarResultHandler
    {
        public override IEnumerable<Type> SupportedTypes
        {
            get { return new[] { typeof(AnyResultOperator) }; }
        }

        protected override object Execute(TopFieldDocs hits)
        {
            return hits.ScoreDocs.Length > 0;
        }
    }
}