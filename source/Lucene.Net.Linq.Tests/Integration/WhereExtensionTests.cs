using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.QueryParsers;
using Lucene.Net.Search;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class WhereExtensionTests : IntegrationTestBase
    {
        IQueryable<SampleDocument> documents;

        protected override Analyzer GetAnalyzer(Net.Util.Version version)
        {
            return new KeywordAnalyzer();
        }

        [SetUp]
        public void AddSampleDocuments()
        {
            AddDocument(new SampleDocument { Name = "Documents Bill", Id = "X.Y.1.2", Version = new Version("1.0"), NullableScalar = 5 });
            AddDocument(new SampleDocument { Name = "Bills Document", Id = "X.Z.1.3", Version = new Version("1.5"), NullableScalar = 1, NumericBool = true});

            documents = provider.AsQueryable<SampleDocument>();
        }

        [Test]
        public void Where()
        {
            var result = documents.Where(new TermQuery(new Term("Name", "Bills Document")));

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }

        [Test]
        public void WhereParseQuery()
        {
            var result = documents.Where(provider.CreateQueryParser<SampleDocument>().Parse("Id:X.Z.1.3 AND Name:\"Bills Document\" AND Version:1.5"));

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }

        [Test]
        public void WhereParseQuery_Numeric()
        {
            var result = documents.Where(provider.CreateQueryParser<SampleDocument>().Parse("NullableScalar:1"));

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }

        [Test]
        public void WhereParseQuery_Numeric_Wildcard()
        {
            var parser = provider.CreateQueryParser<SampleDocument>();
            parser.AllowLeadingWildcard = true;

            var result = documents.Where(parser.Parse("NullableScalar:*"));

            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public void WhereParseQuery_Numeric_Range()
        {
            var result = documents.Where(provider.CreateQueryParser<SampleDocument>().Parse("NullableScalar:[* TO 2]"));

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }

        [Test]
        public void WhereParseQuery_NumericBool_ConvertsFromString()
        {
            var result = documents.Where(provider.CreateQueryParser<SampleDocument>().Parse("NumericBool:true"));

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }

        [Test]
        public void WhereParseQuery_NumericBool_InvalidString_Throws()
        {
            var parser = provider.CreateQueryParser<SampleDocument>();
            TestDelegate call = () => parser.Parse("NumericBool:not-a-bool");
            Assert.That(call, Throws.InstanceOf<ParseException>());
        }

        [Test]
        public void WhereParseQuery_UsesKeyAsDefaultField()
        {
            var parser = provider.CreateQueryParser<SampleDocument>();

            Assert.That(parser.Field, Is.EqualTo("Key"));
        }

        [Test]
        public void WhereParseQuery_OverrideDefaultField()
        {
            var parser = provider.CreateQueryParser<SampleDocument>();
            parser.DefaultSearchProperty = "Id";

            var result = documents.Where(parser.Parse("X.Z.1.3"));

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }
    }
}
