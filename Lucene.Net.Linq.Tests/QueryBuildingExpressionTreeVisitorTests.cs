using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Tests.Integration;
using Lucene.Net.Search;
using Lucene.Net.Util;
using NUnit.Framework;
using Remotion.Linq.Clauses;
using Remotion.Linq.Clauses.Expressions;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class QueryBuildingExpressionTreeVisitorTests_QueryParsing
    {
        private static readonly Version version = new Version("QueryBuildingExpressionTreeVisitorTests_QueryParsing", 0);
        private Analyzer analyzer;
        private QueryBuildingExpressionTreeVisitor builder;
        
        [SetUp]
        public void SetUp()
        {
            analyzer = new PorterStemAnalyzer(version);
            builder = new QueryBuildingExpressionTreeVisitor(new Context(analyzer, version));
        }

        [Test]
        public void UsesPorterStemFilter()
        {
            var query = builder.Parse("Text", "values");

            Assert.That(query.ToString(), Is.EqualTo("Text:valu"));
        }

        [Test]
        public void ParseMultipleTerms()
        {
            var query = builder.Parse("Text", "x y z");
            Assert.That(query.ToString(), Is.EqualTo("Text:x Text:y Text:z"));
        }
    }

    [TestFixture]
    public class QueryBuildingExpressionTreeVisitorTests
    {
        private QueryBuildingExpressionTreeVisitor builder;

        private static readonly QuerySourceReferenceExpression ReferenceExpression =
            new QuerySourceReferenceExpression(new MainFromClause("r", typeof(Record), Expression.Constant("r")));
        private static readonly MemberExpression MemberAccessName =
            Expression.MakeMemberAccess(ReferenceExpression,
            typeof(Record).GetProperty("Name"));
        private static readonly MemberExpression MemberAccessId =
            Expression.MakeMemberAccess(ReferenceExpression,
            typeof(Record).GetProperty("Id"));

        private static readonly Version version = new Version("QueryBuildingExpressionTreeVisitorTests", 0);

        [SetUp]
        public void SetUp()
        {
            builder = new QueryBuildingExpressionTreeVisitor(new Context(new WhitespaceAnalyzer(), version));
        }

        [Test]
        public void DefaultMatchesAllDocs()
        {
            Assert.That(builder.Query.ToString(), Is.EqualTo("*:*"));
        }

        [Test]
        public void BinaryEqualsExpression()
        {
            // where r.Name == "Example"
            builder.VisitExpression(Expression.MakeBinary(
                ExpressionType.Equal,
                MemberAccessName,
                Expression.Constant("Example")));

            Assert.That(builder.Query.ToString(), Is.EqualTo("Name:Example"));
            Assert.That(builder.Query, Is.InstanceOf<TermQuery>());
        }

        [Test]
        public void BinaryEqualsExpression_MemberAccessInQueryValue()
        {
            var searchParams = new Record { Name = "Example" };

            // where r.Name == searchParams.Name
            var expression = Expression.MakeBinary(
                ExpressionType.Equal,
                MemberAccessName,
                Expression.MakeMemberAccess(Expression.Constant(searchParams), typeof(Record).GetProperty("Name")));

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("Name:Example"));
        }

        [Test]
        public void BinaryEqualsExpression_SubPropertyNotSupported()
        {
            // where 0 == r.Name.Length
            var exp = Expression.MakeBinary(
                ExpressionType.Equal,
                Expression.Constant(0),
                Expression.MakeMemberAccess(MemberAccessName, typeof(string).GetProperty("Length")));

            TestDelegate call = () => builder.VisitExpression(exp);
            
            Assert.That(call, Throws.InvalidOperationException);
        }

        [Test]
        public void BinaryEqualsExpression_Transitive()
        {
            // where "Example" == r.Name
            builder.VisitExpression(Expression.MakeBinary(
                ExpressionType.Equal,
                Expression.Constant("Example"),
                MemberAccessName));

            Assert.That(builder.Query.ToString(), Is.EqualTo("Name:Example"));
        }

        [Test]
        public void MethodCallExpression_StartsWith()
        {
            // where r.Name.StartsWith("Example")
            var expression = Expression.Call(MemberAccessName, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), Expression.Constant("Example"));

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("Name:Example*"));
            Assert.That(builder.Query, Is.InstanceOf<PrefixQuery>());
        }

        [Test]
        public void MethodCallExpression_GetField()
        {
            var docRef = new QuerySourceReferenceExpression(new MainFromClause("d", typeof(Document), Expression.Constant("d")));

            // where d.GetField("Name") == "Example"
            var expression =
                Expression.MakeBinary(ExpressionType.Equal, 
                    Expression.Call(docRef, typeof(Document).GetMethod("Get", new[] { typeof(string) }), Expression.Constant("Name")),
                    Expression.Constant("Example"));

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("Name:Example"));
        }

        [Test]
        public void CompoundQuery_And()
        {
            // where r.Name.StartsWith("Example") and r.Id = 100
            var expression = Expression.AndAlso(
                Expression.Call(MemberAccessName, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), Expression.Constant("Example")),
                Expression.MakeBinary(ExpressionType.Equal, MemberAccessId, Expression.Constant(100)));

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("+Name:Example* +Id:" + NumericUtils.IntToPrefixCoded(100)));
        }

        [Test]
        public void CompoundQuery_Or()
        {
            // where r.Name.StartsWith("Example") and r.Id = 100
            var expression = Expression.OrElse(
                Expression.Call(MemberAccessName, typeof(string).GetMethod("StartsWith", new[] { typeof(string) }), Expression.Constant("Example")),
                Expression.MakeBinary(ExpressionType.Equal, MemberAccessId, Expression.Constant(100)));
            
            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("Name:Example* Id:" + NumericUtils.IntToPrefixCoded(100)));
        }

        [Test]
        public void ThrowsOnUnRecognizedExpressionType()
        {
            var expression = (Expression) Expression.MakeBinary(
                ExpressionType.Modulo,
                MemberAccessId,
                Expression.Constant(1));

            TestDelegate call = () => builder.VisitExpression(expression);

            Assert.That(call, Throws.InvalidOperationException);
        }
    }

    public class Record
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

}