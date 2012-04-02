using Lucene.Net.Analysis;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Util;
using Remotion.Linq;
using Remotion.Linq.Clauses;

namespace Lucene.Net.Linq
{
    public class QueryModelTranslator : QueryModelVisitorBase
    {
        private readonly Analyzer analyzer;
        private readonly Version version;
        private Query query;

        public QueryModelTranslator(Analyzer analyzer, Version version)
        {
            this.analyzer = analyzer;
            this.version = version;
        }

        public Query Build(QueryModel queryModel)
        {
            queryModel.Accept(this);
            
            return query ?? new MatchAllDocsQuery();
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            var queryParser = new QueryParser(version, "*", analyzer);
            queryParser.SetLowercaseExpandedTerms(false);
            
            // TODO: test me:
            //queryParser.SetAllowLeadingWildcard(true);

            var visitor = new QueryBuildingExpressionTreeVisitor(queryParser);
            visitor.VisitExpression(whereClause.Predicate);
            query = visitor.Query;
        }
    }
}