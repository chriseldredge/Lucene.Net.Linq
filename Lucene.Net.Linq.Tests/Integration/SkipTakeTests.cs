using System;
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class SkipTakeTests : IntegrationTestBase
    {
        private IQueryable<string> docNames;

        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "c", Scalar = 3, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new SampleDocument { Name = "a", Scalar = 1, Version = new Version(20, 0, 0) });
            AddDocument(new SampleDocument { Name = "b", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });

            var documents = provider.AsMappedQueryable<SampleDocument>();
            docNames = from doc in documents orderby doc.Name select doc.Name;
        }

        [Test]
        public void SkipTwo()
        {
            Assert.That(docNames.Skip(2).ToArray(), Is.EqualTo(new[] { "c" }));
        }

        [Test]
        public void SkipOne_TakeOne()
        {
           Assert.That(docNames.Skip(1).Take(1).ToArray(), Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void TakeTwo_SkipOne()
        {
            Assert.That(docNames.Take(2).Skip(1).ToArray(), Is.EqualTo(new[] { "b" }));
        }

        [Test]
        public void TakeOne()
        {
            Assert.That(docNames.Take(1).ToArray(), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void LowestTakeWins()
        {
            Assert.That(docNames.Take(1).Take(3).ToArray(), Is.EqualTo(new[] { "a" }));
        }

        [Test]
        public void MultipleSkip()
        {
            Assert.That(docNames.Skip(1).Skip(1).ToArray(), Is.EqualTo(new[] { "c" }));
        }

        [Test]
        public void MultipleSkipWithTake()
        {
            Assert.That(docNames.Skip(1).Take(2).Skip(1).ToArray(), Is.EqualTo(new[] { "c" }));
        }
    }
}