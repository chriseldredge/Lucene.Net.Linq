using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Expressions;
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
    public class QueryBuildingExpressionTreeVisitorTests : IFieldMappingInfoProvider
    {
        private QueryBuildingExpressionTreeVisitor builder;

        private static readonly Expression MemberAccessId =
            new LuceneQueryFieldExpression(typeof (int), "Id");

        private static readonly Version version = Version.LUCENE_29;

        [SetUp]
        public void SetUp()
        {
            builder = new QueryBuildingExpressionTreeVisitor(new Context(new RAMDirectory(), new WhitespaceAnalyzer(), version, null, new object()), this);
        }

        public IFieldMappingInfo GetMappingInfo(string propertyName)
        {
            return new FakeFieldMappingInfo {FieldName = propertyName};
        }

        class FakeFieldMappingInfo : IFieldMappingInfo
        {
            public string FieldName { get; set; }
            public TypeConverter Converter { get; set; }
            public PropertyInfo PropertyInfo { get; set; }
            public string ConvertToQueryExpression(object value)
            {
                return value.ToString();
            }

            public bool IsNumericField
            {
                get { return true; }
            }
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
            var expression = new LuceneQueryExpression(
                new LuceneQueryFieldExpression(typeof (int), "Count"),
                Expression.Constant(5),
                BooleanClause.Occur.MUST,
                QueryType.GreaterThan);

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("+Count:{5 TO " + int.MaxValue + "]"));
        }

        [Test]
        public void GreaterThanOrEqual()
        {
            var expression = new LuceneQueryExpression(
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

            var expression = new LuceneQueryExpression(
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
            var expression = new LuceneQueryExpression(
                new LuceneQueryFieldExpression(typeof(DateTime), "Average"),
                Expression.Constant(11.5d),
                BooleanClause.Occur.MUST,
                QueryType.LessThanOrEqual);

            builder.VisitExpression(expression);

            Assert.That(builder.Query.ToString(), Is.EqualTo("+Average:[" + double.MinValue + " TO 11.5]"));
        }
    }

    public class Record
    {
        public string Name { get; set; }
        public int Id { get; set; }
    }

}