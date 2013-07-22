using System;
using System.ComponentModel;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Linq.Tests.Integration;
using Lucene.Net.Search;
using NUnit.Framework;
using Lucene.Net.QueryParsers;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class ReflectionFieldMapperTests
    {
        [Test]
        public void SpecifyAnalyzer()
        {
            var mapper = CreateMapper("Text", analyzer: new PorterStemAnalyzer(Net.Util.Version.LUCENE_30));

            var query = mapper.CreateQuery("values");

            Assert.That(query.ToString(), Is.EqualTo("Text:valu"));
        }

        [Test]
        public void ParseMultipleTerms()
        {
            var mapper = CreateMapper("Text", analyzer: new StandardAnalyzer(Net.Util.Version.LUCENE_30));

            var query = mapper.CreateQuery("x y z");
            Assert.That(query.ToString(), Is.EqualTo("Text:x Text:y Text:z"));
        }

        [Test]
        public void ParseKeywordWithWhitespace()
        {
            var mapper = CreateMapper("Text");

            var query = mapper.CreateQuery("x y z");
            Assert.That(query.ToString(), Is.EqualTo("Text:x y z"));
        }

        [Test]
        public void ParseMultipleTermsWithDefaultOperatorAnd()
        {
            var mapper = CreateMapper("Text", analyzer: new StandardAnalyzer(Net.Util.Version.LUCENE_30), defaultParseOperaor: QueryParser.Operator.AND);

            var query = mapper.CreateQuery("x y z");
            Assert.That(query.ToString(), Is.EqualTo("+Text:x +Text:y +Text:z"));
        }

        [Test]
        public void ParseLowercaseExpandedTerms()
        {
            var mapper = CreateMapper("Text");

            var query = mapper.CreateQuery("FOO*");

            Assert.That(query.ToString(), Is.EqualTo("Text:foo*"));
        }

        [Test]
        public void ParseDoNotLowercaseExpandedTerms()
        {
            var mapper = CreateMapper("Text", caseSensitive: true);

            var query = mapper.CreateQuery("FOO*");

            Assert.That(query.ToString(), Is.EqualTo("Text:FOO*"));
        }

        [Test]
        public void RangeQuery()
        {
            var mapper = CreateMapper("Text");

            var result = mapper.CreateRangeQuery("a", "z", RangeType.Inclusive, RangeType.Inclusive);

            Assert.That(result, Is.InstanceOf<TermRangeQuery>());
            Assert.That(result.ToString(), Is.EqualTo("Text:[a TO z]"));
        }

        [Test]
        public void RangeQueryComplexType()
        {
            var mapper = CreateMapper("Version", new VersionConverter());

            var result = mapper.CreateRangeQuery(new Version("2.0"), null, RangeType.Exclusive, RangeType.Inclusive);

            Assert.That(result, Is.InstanceOf<TermRangeQuery>());
            Assert.That(result.ToString(), Is.EqualTo("Version:{2.0 TO *]"));
        }

        [Test]
        public void AnalyzesQueryValue()
        {
            var mapper = CreateMapper("Text", analyzer: new StandardAnalyzer(Net.Util.Version.LUCENE_30));

            var result = mapper.CreateRangeQuery("SomeValue", null, RangeType.Inclusive, RangeType.Inclusive);

            Assert.That(result, Is.InstanceOf<TermRangeQuery>());
            Assert.That(result.ToString(), Is.EqualTo("Text:[somevalue TO *]"));
        }

        [Test]
        public void CustomSort()
        {
            var mapper = CreateMapper("Version", analyzer: new KeywordAnalyzer(), converter: new VersionConverter());

            var sort = mapper.CreateSortField(reverse: false);

            Assert.That(sort.Field, Is.EqualTo(mapper.FieldName));
            Assert.That(sort.ComparatorSource, Is.InstanceOf<NonGenericConvertableFieldComparatorSource>());

        }

        public string Text { get; set; }

        public string Version { get; set; }

        public string ReadOnly { get { return "You can't write to me"; } }

        [Test]
        public void CopyFromDocument_ReadOnlyProperty()
        {
            var mapper = CreateMapper("ReadOnly");

            TestDelegate call = () => mapper.CopyFromDocument(new Document(), new QueryExecutionContext(), this);

            Assert.That(call, Throws.Nothing);
        }

        private ReflectionFieldMapper<ReflectionFieldMapperTests> CreateMapper(string propertyName, TypeConverter converter = null, Analyzer analyzer = null, QueryParser.Operator defaultParseOperaor = QueryParser.Operator.OR, bool caseSensitive = false)
        {
            return new ReflectionFieldMapper<ReflectionFieldMapperTests>(
                typeof(ReflectionFieldMapperTests).GetProperty(propertyName),
                StoreMode.Yes,
                IndexMode.Analyzed, TermVectorMode.No, converter, propertyName, defaultParseOperaor, caseSensitive, analyzer ?? new KeywordAnalyzer(), 1f);

        }

    }
}
