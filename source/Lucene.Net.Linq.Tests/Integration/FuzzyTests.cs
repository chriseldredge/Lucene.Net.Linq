using System;
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class FuzzyTests : IntegrationTestBase
    {
        private IQueryable<SampleDocument> documents;

        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "banana", Scalar = 3, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new SampleDocument { Name = "orange", Scalar = 1, Flag = false, Version = new Version(20, 0, 0) });
            AddDocument(new SampleDocument { Name = "strawberry", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });

            documents = provider.AsQueryable<SampleDocument>();
        }

        [Test]
        public void EqualFuzzy()
        {
            var result = documents
                .Where(d =>
                    (d.Name == "bananna").Fuzzy(0.6f))
                .FirstOrDefault();

            Assert.That(result, Is.Not.Null);
            Assert.That(result.Name, Is.EqualTo("banana"));
        }

        [Test]
        public void MultipleEqualFuzzy()
        {
            var result = documents
                .Where(d =>
                    (d.Name == "bananna").Fuzzy(0.6f) ||
                    (d.Name == "strawbery").Fuzzy(0.7f))
                .ToList()
                .OrderBy(x => x.Name)
                .ToList();

            Assert.That(result.Count, Is.EqualTo(2));
            Assert.That(result[0].Name, Is.EqualTo("banana"));
            Assert.That(result[1].Name, Is.EqualTo("strawberry"));
        }

        [Test]
        public void MultipleEqualFuzzy2()
        {
            var result = documents
                .Where(d =>
                    (d.Name == "bananna").Fuzzy(0.6f) ||
                    (d.Name == "strawbery").Fuzzy(0.9f))
                .ToList()
                .OrderBy(x => x.Name)
                .ToList();

            Assert.That(result.Count, Is.EqualTo(1));
            Assert.That(result[0].Name, Is.EqualTo("banana"));
        }
    }
}
