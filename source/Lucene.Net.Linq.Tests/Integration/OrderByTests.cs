using System;
using System.Linq;
using Lucene.Net.Analysis;
using NUnit.Framework;

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

        [Test]
        public void OrderBy_ComparableGeneric()
        {
            writer.DeleteAll();

            AddDocument(new SampleDocument { GenericComparable = new SampleGenericOnlyComparable(23155163) });
            AddDocument(new SampleDocument { GenericComparable = new SampleGenericOnlyComparable(4667) });
            AddDocument(new SampleDocument { GenericComparable = new SampleGenericOnlyComparable(22468359) });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents where d.GenericComparable != null orderby d.GenericComparable select d.GenericComparable.Value;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 4667, 22468359, 23155163 }));
        }

        [Test]
        public void OrderBy_Score()
        {
            AddTestScoreDocuments();

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents where d.Name == "apple" orderby d.Score() select d.Scalar;

            Assert.That(result.ToArray(), Is.EqualTo(new[] {3, 2, 1}));
        }

        [Test]
        public void OrderBy_Score_Desc()
        {
            AddTestScoreDocuments();

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents where d.Name == "apple" orderby d.Score() descending select d.Scalar;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }

        [Test]
        public void OrderBy_ScoreProperty()
        {
            AddTestScoreDocuments();

            var documents = provider.AsQueryable<ScoreTrackingSampleDocument>();

            var result = from d in documents where d.Name == "apple" orderby d.ScoreProperty select d.Scalar;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 3, 2, 1 }));
        }

        [Test]
        public void OrderBy_ScoreProperty_Desc()
        {
            AddTestScoreDocuments();

            var documents = provider.AsQueryable<ScoreTrackingSampleDocument>();

            var result = from d in documents where d.Name == "apple" orderby d.ScoreProperty descending select d.Scalar;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 1, 2, 3 }));
        }
        [Test]
        public void OrderBy_Score_ExtensionMethod()
        {
            AddTestScoreDocuments();

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from d in documents where d.Name == "apple" select d.Scalar;

            Assert.That(result.OrderBy(d => d.Score()).ToArray(), Is.EqualTo(new[] { 3, 2, 1 }));
        }

        private void AddTestScoreDocuments()
        {
            AddDocument(new SampleDocument { Name = "apple apple apple apple apple apple ", Scalar = 3, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new SampleDocument { Name = "banana banana banana banana apple banana banana banana ", Scalar = 1, Flag = false, Version = new Version(20, 0, 0) });
            AddDocument(new SampleDocument { Name = "apple pie apple sauce", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });
        }
    }
}
