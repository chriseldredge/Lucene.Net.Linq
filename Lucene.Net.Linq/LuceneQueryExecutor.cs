using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;
using Remotion.Linq.Parsing;
using Remotion.Linq.Parsing.ExpressionTreeVisitors;

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
            var builder = new QueryBuilder(queryModel);
            var query = builder.Build();

            var mapping = new QuerySourceMapping();
            mapping.AddMapping(queryModel.MainFromClause, Expression.Property(Expression.Constant(this), "CurrentDocument"));
            queryModel.TransformExpressions(e => ReferenceReplacingExpressionTreeVisitor.ReplaceClauseReferences(e, mapping, true));

            var projection = GetProjector<T>(queryModel);
            var projector = projection.Compile();

            using (var searcher = new IndexSearcher(directory, true))
            {
                var hits = searcher.Search(query);
                
                for (var i = 0; i < hits.Length(); i++)
                {
                    // TODO:
                    //if (reader.IsDeleted(i)) continue;

                    CurrentDocument = hits.Doc(i);
                    yield return projector(CurrentDocument);
                }
            }
        }

        public Expression<Func<Document, T>> GetProjector<T>(QueryModel queryModel)
        {
            return Expression.Lambda<Func<Document, T>>(queryModel.SelectClause.Selector, Expression.Parameter(typeof(Document)));
        }
    }

    public class QueryBuilder : QueryModelVisitorBase
    {
        private readonly QueryModel queryModel;
        private Query query;

        public QueryBuilder(QueryModel queryModel)
        {
            this.queryModel = queryModel;
        }

        public Query Build()
        {
            queryModel.Accept(this);
            
            return query ?? new MatchAllDocsQuery();
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            query = QueryBuildingExpressionTreeVisitor.ParseQueryFromWhereClause(whereClause);
        }
    }

    public class QueryBuildingExpressionTreeVisitor : ExpressionTreeVisitor
    {
        private Query Query { get; set; }
        private string fieldName;
        private string queryValue;
        private bool topVisited;

        protected QueryBuildingExpressionTreeVisitor()
        {
            fieldName = "id";
        }

        public static Query ParseQueryFromWhereClause(WhereClause whereClause)
        {
            var instance = new QueryBuildingExpressionTreeVisitor();

            instance.VisitExpression(whereClause.Predicate);

            return instance.Query;
        }

        public override Expression VisitExpression(Expression expression)
        {
            if (topVisited)
            {
                return base.VisitExpression(expression);
            }

            topVisited = true;

            Expression result;

            if (IsContainsMethod(expression))
            {
                result = base.VisitExpression(expression);
                var parser = new QueryParser(fieldName, new StandardAnalyzer());
                Query = parser.Parse(queryValue);        
            }
            else if (expression is BinaryExpression)
            {
                result = base.VisitExpression(expression);
                Query = new TermQuery(new Term(fieldName, queryValue));
            }
            else
            {
                throw new InvalidOperationException("Unsupported where clause.");
            }
            
            return result;
        }

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            queryValue = Expression.Lambda<Func<string>>(expression.Right).Compile()();
            return base.VisitBinaryExpression(expression);
        }

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            queryValue = Expression.Lambda<Func<string>>(expression).Compile()();
            return base.VisitConstantExpression(expression);
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (IsContainsMethod(expression))
            {
                fieldName = "text";
                queryValue = (string)Expression.Lambda(expression.Arguments[0]).Compile().DynamicInvoke();
            }
            else
            {
                fieldName = (string)Expression.Lambda(expression.Arguments[0]).Compile().DynamicInvoke();    
            }
            return base.VisitMethodCallExpression(expression);
        }

        private static bool IsContainsMethod(Expression expression)
        {
            var methodExpression = expression as MethodCallExpression;
            return methodExpression != null && methodExpression.Method.Name == "Contains";
        }
    }
}