using System;
using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Linq.Tests.Integration;
using Lucene.Net.Search;
using NUnit.Framework;
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

        private static readonly Expression MemberAccessName =
            new LuceneQueryFieldExpression(typeof(string), "Name");

        private static readonly Expression MemberAccessId =
            new LuceneQueryFieldExpression(typeof (int), "Id");

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
        public void BinaryNotEqualsExpression()
        {
            // where r.Name != "Example"
            var expression = Expression.MakeBinary(
                ExpressionType.NotEqual,
                MemberAccessName,
                Expression.Constant("Example"));

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("-Name:Example *:*"));
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
        public void ThrowsOnUnRecognizedExpressionType()
        {
            var expression = (Expression) Expression.MakeBinary(
                ExpressionType.Modulo,
                MemberAccessId,
                Expression.Constant(1));

            TestDelegate call = () => builder.VisitExpression(expression);

            Assert.That(call, Throws.Exception.InstanceOf<NotSupportedException>());
        }
    }

    public class Record
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

}