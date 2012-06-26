using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Clauses.Expressions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Tests.Integration;
using Lucene.Net.Linq.Translation.TreeVisitors;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Translation.TreeVisitors
{
    [TestFixture]
    public class QueryBuildingExpressionTreeVisitorTests_QueryParsing
    {
        private static readonly Version version = Version.LUCENE_29;
        private Analyzer analyzer;
        private QueryBuildingExpressionTreeVisitor builder;
        
        [SetUp]
        public void SetUp()
        {
            analyzer = new PorterStemAnalyzer(version);
            builder = new QueryBuildingExpressionTreeVisitor(new Context(new RAMDirectory(), analyzer, version, null, new object()), null);
        }

        [Test]
        public void UsesPorterStemFilter()
        {
            var query = builder.Parse(new FakeFieldMappingInfo {FieldName = "Text"}, "values");

            Assert.That(query.ToString(), Is.EqualTo("Text:valu"));
        }

        [Test]
        public void ParseMultipleTerms()
        {
            var query = builder.Parse(new FakeFieldMappingInfo { FieldName = "Text" }, "x y z");
            Assert.That(query.ToString(), Is.EqualTo("Text:x Text:y Text:z"));
        }

        [Test]
        public void ParseLowercaseExpandedTerms()
        {
            var query = builder.Parse(new FakeFieldMappingInfo { FieldName = "Text", CaseSensitive = false }, "FOO*");
            Assert.That(query.ToString(), Is.EqualTo("Text:foo*"));
        }

        [Test]
        public void ParseDoNotLowercaseExpandedTerms()
        {
            var query = builder.Parse(new FakeFieldMappingInfo { FieldName = "Text", CaseSensitive = true }, "FOO*");
            Assert.That(query.ToString(), Is.EqualTo("Text:FOO*"));
        }
    }

    internal class FieldMappingInfoProviderStub : IFieldMappingInfoProvider
    {
        public IFieldMappingInfo GetMappingInfo(string propertyName)
        {
            return new FakeFieldMappingInfo { FieldName = propertyName, IsNumericField = IsNumeric };
        }

        public IEnumerable<string> AllFields
        {
            get { return new[] { "Id" }; }
        }

        public bool IsNumeric { get; set; }
    }

    [TestFixture]
    public class QueryBuildingExpressionTreeVisitorTests
    {
        private QueryBuildingExpressionTreeVisitor builder;

        private static readonly Expression MemberAccessId =
            new LuceneQueryFieldExpression(typeof (int), "Id");

        private static readonly Version version = Version.LUCENE_29;
        private FieldMappingInfoProviderStub fieldMappingInfoProvider;

        [SetUp]
        public void SetUp()
        {
            fieldMappingInfoProvider = new FieldMappingInfoProviderStub { IsNumeric = true };
            builder = new QueryBuildingExpressionTreeVisitor(new Context(new RAMDirectory(), new LowercaseKeywordAnalyzer(), version, null, new object()), fieldMappingInfoProvider);
        }

        [Test]
        public void DefaultMatchesAllDocs()
        {
            Assert.That(builder.Query.ToString(), Is.EqualTo("*:*"));
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

        [Test]
        public void GreaterThan()
        {
            var expression = new LuceneQueryPredicateExpression(
                new LuceneQueryFieldExpression(typeof (int), "Count"),
                Expression.Constant(5),
                BooleanClause.Occur.MUST,
                QueryType.GreaterThan);

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("+Count:{5 TO " + int.MaxValue + "]"));
        }

        [Test]
        public void GreaterThan_AnalyzesTerm()
        {
            fieldMappingInfoProvider.IsNumeric = false;

            var expression = new LuceneQueryPredicateExpression(
                new LuceneQueryFieldExpression(typeof(string), "Name"),
                Expression.Constant("SampleName"),
                BooleanClause.Occur.MUST,
                QueryType.GreaterThan);

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("+Name:{samplename TO *]"));
        }

        [Test]
        public void GreaterThanOrEqual()
        {
            var expression = new LuceneQueryPredicateExpression(
                new LuceneQueryFieldExpression(typeof(float), "Count"),
                Expression.Constant(6f),
                BooleanClause.Occur.MUST,
                QueryType.GreaterThanOrEqual);

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("+Count:[6 TO " + float.MaxValue + "]"));
        }

        [Test]
        public void LessThan_DateTime()
        {
            var dateTime = new DateTime(2012, 4, 18, 11, 22, 33);

            var expression = new LuceneQueryPredicateExpression(
                new LuceneQueryFieldExpression(typeof(DateTime), "Published"),
                Expression.Constant(dateTime),
                BooleanClause.Occur.MUST,
                QueryType.LessThan);

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("+Published:[" + DateTime.MinValue.ToUniversalTime().Ticks + " TO " + dateTime.ToUniversalTime().Ticks + "}"));
        }

        [Test]
        public void LessThanOrEqual()
        {
            var expression = new LuceneQueryPredicateExpression(
                new LuceneQueryFieldExpression(typeof(DateTime), "Average"),
                Expression.Constant(11.5d),
                BooleanClause.Occur.MUST,
                QueryType.LessThanOrEqual);

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("+Average:[" + double.MinValue + " TO 11.5]"));
        }
    }
}