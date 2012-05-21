using System;
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class SingleResultTests : IntegrationTestBase
    {
        private IQueryable<SampleDocument> documents;

        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "c", Scalar = 3, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new SampleDocument { Name = "a", Scalar = 1, Version = new Version(20, 0, 0) });
            AddDocument(new SampleDocument { Name = "b", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });

            documents = provider.AsQueryable<SampleDocument>();
        }

        [Test]
        public void First()
        {
            Assert.That(documents.First().Name, Is.EqualTo("c"));
        }

        [Test]
        public void Single()
        {
            Assert.That(documents.Single(d => d.Name == "c").Name, Is.EqualTo("c"));
        }

        [Test]
        public void SingleOrDefault()
        {
            Assert.That(documents.SingleOrDefault(d => d.Name == "nonesuch"), Is.Null);
        }

    }
}