using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class BoostTests : IntegrationTestBase
    {
        private IQueryable<SampleDocument> documents;

        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "sample", Id = "0", Scalar = 0});
            AddDocument(new SampleDocument { Name = "sample", Id = "1", Scalar = 1});

            documents = provider.AsQueryable<SampleDocument>();
        }

        [Test]
        public void Single()
        {
            var first = documents.Where(d => d.Name == "sample").Boost(d => d.Scalar);

            Assert.That(first.ToList()[0].Id, Is.EqualTo("1"));
        }

        [Test]
        public void Multiple()
        {
            AddDocument(new SampleDocument { Name = "sample", Id = "33", Scalar = 1 });

            var first = documents.Where(d => d.Name == "sample").Boost(d => d.Id.Length).Boost(d => d.Scalar);

            Assert.That(first.ToList()[0].Id, Is.EqualTo("33"));
        }
    }
}