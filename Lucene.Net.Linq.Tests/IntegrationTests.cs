using System;
using System.Linq;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class IntegrationTests
    {
        private LuceneDataProvider provider;
        private Directory directory;
        private IndexWriter writer;
        private static readonly Version replaceInvalidAcronym = new Version("test", 0);

        [SetUp]
        public void SetUp()
        {
            directory = new RAMDirectory();
            writer = new IndexWriter(directory, new StandardAnalyzer(replaceInvalidAcronym), IndexWriter.MaxFieldLength.UNLIMITED);
            provider = new LuceneDataProvider(directory);
        }

        [Test]
        public void SelectDocument()
        {
            AddDocument("a");

            var sample = provider.AsQueryable();

            var result = from s in sample select s;

            Assert.That(result.First().Get("id"), Is.EqualTo("a"));
        }

        [Test]
        public void SelectWithIdentityMethod()
        {
            AddDocument("a");
            AddDocument("b");

            var sample = provider.AsQueryable();

            var result = from s in sample select IdentityMethod(s);

            Assert.That(result.ToArray().Select(d => d.Get("id")), Is.EqualTo(new[] {"a", "b"}));
        }

        [Test]
        public void SelectWithTransformingMethod()
        {
            AddDocument("a");

            var sample = provider.AsQueryable();

            var result = from s in sample select TransformingMethod(s);

            Assert.That(result.First(), Is.EqualTo("a"));
        }

        [Test]
        public void SelectField()
        {
            AddDocument("a");

            var sample = provider.AsQueryable();

            var result = from s in sample select s.Get("id");

            Assert.That(result.First(), Is.EqualTo("a"));
        }

        [Test]
        public void SelectComplex()
        {
            AddDocument("a");

            var sample = provider.AsQueryable();

            var result = from s in sample select s.Get("id") + " suffix";

            Assert.That(result.First(), Is.EqualTo("a suffix"));
        }

        private static Document IdentityMethod(Document document)
        {
            return document;
        }

        private static object TransformingMethod(Document document)
        {
            return document.Get("id");
        }

        private Document AddDocument(string id)
        {
            var doc = new Document();
            doc.Add(new Field("id", id, Field.Store.YES, Field.Index.ANALYZED_NO_NORMS));

            writer.AddDocument(doc);
            writer.Commit();

            return doc;
        }
    }

}
