using System.Linq;
using Lucene.Net.Analysis;
using NUnit.Framework;
using Version = System.Version;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class OrderByTests : IntegrationTestBase
    {
        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "c", Scalar = 3, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new SampleDocument { Name = "a", Scalar = 1, Flag = false, Version = new Version(20, 0, 0) });
            AddDocument(new SampleDocument { Name = "b", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });
        }

        protected override Analyzer GetAnalyzer(Net.Util.Version version)
        {
            var a = new PerFieldAnalyzerWrapper(base.GetAnalyzer(version));
            a.AddAnalyzer("Version", new KeywordAnalyzer());
            a.AddAnalyzer("Flag", new KeywordAnalyzer());
            return a;
        }

        [Test]
        public void OrderBy_String()
        {
            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents orderby d.Name select d.Name;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void OrderBy_String_Desc()
        {
            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents orderby d.Name descending select d.Name;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { "c", "b", "a" }));
        }

        [Test]
        public void OrderBy_Int()
        {
            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents orderby d.Scalar select d.Scalar;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void OrderBy_Long()
        {
            writer.DeleteAll();

            AddDocument(new SampleDocument { Long = 23155163 });
            AddDocument(new SampleDocument { Long = 4667 });
            AddDocument(new SampleDocument { Long = 22468359 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents orderby d.Long select d.Long;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 4667L, 22468359L, 23155163L }));
        }

        [Test]
        public void OrderBy_Bool()
        {
            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents orderby d.Flag select d.Flag;

            Assert.That(result.ToArray(), Is.EqualTo(new [] { false, true, true }));
        }

        [Test]
        public void OrderBy_Comparable()
        {
            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents orderby d.Version select d.Version.Major;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 3, 20, 100 }));
        }
    }
}
