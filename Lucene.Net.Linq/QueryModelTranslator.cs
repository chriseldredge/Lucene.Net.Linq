using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq
{
    internal class Context
    {
        private readonly Analyzer analyzer;
        private readonly Version version;

        public Context(Analyzer analyzer, Version version)
        {
            this.analyzer = analyzer;
            this.version = version;
        }

        public Analyzer Analyzer
        {
            get { return analyzer; }
        }

        public Version Version
        {
            get { return version; }
        }
    }

    public class QueryModelTranslator : QueryModelVisitorBase
    {
        private readonly Context context;
        private Query query;

        internal QueryModelTranslator(Context context)
        {
            this.context = context;
        }

        public Query Build(QueryModel queryModel)
        {
            queryModel.Accept(this);
            
            return query ?? new MatchAllDocsQuery();
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            var visitor = new QueryBuildingExpressionTreeVisitor(context);
            visitor.VisitExpression(whereClause.Predicate);
            query = visitor.Query;
        }
    }
}