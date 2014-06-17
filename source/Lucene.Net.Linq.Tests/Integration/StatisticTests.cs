using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class StatisticTests : IntegrationTestBase
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
        public void CountsTotalHits()
        {
            LuceneQueryStatistics stats = null;

            documents.CaptureStatistics(s => { stats = s; }).Skip(1).Take(1).Count();

            Assert.That(stats, Is.Not.Null, "stats");
            Assert.That(stats.TotalHits, Is.EqualTo(documents.Count()));
        }

        [Test]
        public void InvokesOncePerExecution()
        {
            var list = new List<LuceneQueryStatistics>();

            documents = documents.CaptureStatistics(list.Add);

            documents.Where(doc => doc.Scalar == 1).ToList();
            documents.Where(doc => doc.Scalar != 1).ToList();

            Assert.That(list.Count, Is.EqualTo(2));
            Assert.That(list[0].TotalHits, Is.EqualTo(1));
            Assert.That(list[1].TotalHits, Is.EqualTo(2));
        }
    }
}