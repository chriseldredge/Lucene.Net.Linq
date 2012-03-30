using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using Remotion.Linq;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.ExpressionTreeVisitors;
using Remotion.Linq.Parsing;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq
{
    internal class DocumentQueryExecutor : LuceneQueryExecutor<Document>
    {
        public DocumentQueryExecutor(Directory directory, Analyzer analyzer, Version version) : base(directory, analyzer, version)
        {
        }

        protected override void SetCurrentDocument(Document doc)
        {
            CurrentDocument = doc;
        }
    }

    internal class DocumentHolderQueryExecutor<TDocument> : LuceneQueryExecutor<TDocument> where TDocument : IDocumentHolder
    {
        private readonly Func<TDocument> documentFactory;

        public DocumentHolderQueryExecutor(Directory directory, Analyzer analyzer, Version version, Func<TDocument> documentFactory) : base(directory, analyzer, version)
        {
            this.documentFactory = documentFactory;
        }

        protected override void  SetCurrentDocument(Document doc)
        {
            var holder = documentFactory();
            holder.Document = doc;

            CurrentDocument = holder;
        }
    }

    internal abstract class LuceneQueryExecutor<TDocument> : IQueryExecutor
    {
        private readonly Directory directory;
        private readonly Analyzer analyzer;
        private readonly Version version;

        public TDocument CurrentDocument { get; protected set; }

        protected LuceneQueryExecutor(Directory directory, Analyzer analyzer, Version version)
        {
            this.directory = directory;
            this.analyzer = analyzer;
            this.version = version;
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
            var builder = new QueryBuilder(queryModel, analyzer, version);
            var query = builder.Build();

            var mapping = new QuerySourceMapping();
            mapping.AddMapping(queryModel.MainFromClause, GetCurrentRowExpression());
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

                    SetCurrentDocument(hits.Doc(i));
                    yield return projector(CurrentDocument);
                }
            }
        }

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

    public class QueryBuilder : QueryModelVisitorBase
    {
        private readonly QueryModel queryModel;
        private readonly Analyzer analyzer;
        private readonly Version version;
        private Query query;

        public QueryBuilder(QueryModel queryModel, Analyzer analyzer, Version version)
        {
            this.queryModel = queryModel;
            this.analyzer = analyzer;
            this.version = version;
        }

        public Query Build()
        {
            queryModel.Accept(this);
            
            return query ?? new MatchAllDocsQuery();
        }

        public override void VisitWhereClause(WhereClause whereClause, QueryModel queryModel, int index)
        {
            query = QueryBuildingExpressionTreeVisitor.ParseQueryFromWhereClause(whereClause, analyzer, version);
        }
    }

    public class QueryBuildingExpressionTreeVisitor : ExpressionTreeVisitor
    {
        private readonly Analyzer analyzer;
        private readonly Version version;
        private Query Query { get; set; }
        private string fieldName;
        private string queryValue;
        private bool topVisited;

        protected QueryBuildingExpressionTreeVisitor(Analyzer analyzer, Version version)
        {
            this.analyzer = analyzer;
            this.version = version;
        }

        public static Query ParseQueryFromWhereClause(WhereClause whereClause, Analyzer analyzer, Version version)
        {
            var instance = new QueryBuildingExpressionTreeVisitor(analyzer, version);

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
                var parser = new QueryParser(version, fieldName, analyzer);
                Query = parser.Parse(queryValue);        
            }
            else if (expression is BinaryExpression)
            {
                result = base.VisitExpression(expression);
                Query = new TermQuery(new Term(fieldName, queryValue));
            }
            else
            {
                throw new InvalidOperationException("Unsupported where clause expression " + expression.NodeType);
            }
            
            return result;
        }

        protected override Expression VisitBinaryExpression(BinaryExpression expression)
        {
            queryValue = EvaluateExpression(expression.Right);
            return base.VisitBinaryExpression(expression);
        }

        protected override Expression VisitConstantExpression(ConstantExpression expression)
        {
            queryValue = EvaluateExpression(expression);
            return base.VisitConstantExpression(expression);
        }

        protected override Expression VisitMemberExpression(MemberExpression expression)
        {
            fieldName = expression.Member.Name;
            return base.VisitMemberExpression(expression);
        }

        protected override Expression VisitMethodCallExpression(MethodCallExpression expression)
        {
            if (IsContainsMethod(expression))
            {
                queryValue = EvaluateExpression(expression.Arguments[0]);
            }
            else
            {
                fieldName = EvaluateExpression(expression.Arguments[0]);
            }
            return base.VisitMethodCallExpression(expression);
        }

        private static bool IsContainsMethod(Expression expression)
        {
            var methodExpression = expression as MethodCallExpression;
            return methodExpression != null && methodExpression.Method.Name == "Contains";
        }

        private static string EvaluateExpression(Expression expression)
        {
            var lambda = Expression.Lambda(expression).Compile();
            var result = lambda.DynamicInvoke();
            if (result is ValueType)
            {
                return ConvertToPrefixCoded((ValueType) result);
            }
            return result == null ? null : result.ToString();
        }

        private static string ConvertToPrefixCoded(ValueType result)
        {
            if (result is Int32)
            {
                return NumericUtils.IntToPrefixCoded((int) result);
            }

            throw new InvalidOperationException("ValueType " + result.GetType() + " not supported.");
        }
    }
}