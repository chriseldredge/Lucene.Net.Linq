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
        public void First_AfterSkip()
        {
            Assert.That(documents.Skip(1).First().Name, Is.EqualTo("a"));
        }

        [Test]
        public void Last()
        {
            Assert.That(documents.OrderByDescending(d => d.Name).Last().Name, Is.EqualTo("a"));
        }

        [Test]
        public void Last_AfterTake()
        {
            Assert.That(documents.OrderByDescending(d => d.Name).Take(2).Last().Name, Is.EqualTo("b"));
        }

        [Test]
        public void LastOrDefault()
        {
            Assert.That(documents.OrderByDescending(d => d.Name).LastOrDefault(d => d.Name == "nonesuch"), Is.Null);
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