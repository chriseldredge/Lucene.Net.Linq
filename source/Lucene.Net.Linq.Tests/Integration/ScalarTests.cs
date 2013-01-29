using System;
using System.Linq;
using System.Threading;
using Lucene.Net.Linq.Util;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class ScalarTests : IntegrationTestBase
    {
        private IQueryable<SampleDocument> documents;

        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new SampleDocument { Name = "c", Scalar = 1, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new SampleDocument { Name = "a", Scalar = 3, Version = new Version(20, 0, 0) });
            AddDocument(new SampleDocument { Name = "b", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });

            documents = provider.AsQueryable<SampleDocument>();
        }

        [Test]
        public void Any()
        {
            Assert.That(documents.Any(), Is.True);
        }

        [Test]
        public void Any_Not()
        {
            Assert.That(documents.Any(d => d.Name == "nonesuch"), Is.False);
        }

        [Test]
        public void Count()
        {
            Assert.That(documents.Count(), Is.EqualTo(3), "Count()");
        }

        [Test]
        public void CountLong()
        {
            Assert.That(documents.LongCount(), Is.EqualTo(3L), "LongCount()");
        }

        [Test]
        public void CountWhere()
        {
            Assert.That(documents.Count(d => d.Flag), Is.EqualTo(2), "Count()");
        }

        [Test]
        public void CountAfterSkip()
        {
            Assert.That(documents.Skip(1).Count(), Is.EqualTo(2), "Skip(1).Count()");
        }

        [Test]
        public void Max()
        {
            Assert.That(documents.Max(d => d.Scalar), Is.EqualTo(3));
        }

        [Test]
        public void Max_Version()
        {
            Assert.That(documents.Select(d => d.Version).Max(), Is.EqualTo(new Version(100, 0, 0)));
        }

        [Test]
        public void Max_OverridesPreviousSorts()
        {
            Assert.That(documents.OrderBy(d => d.Version).Max(d => d.Scalar), Is.EqualTo(3));
        }

        [Test]
        public void Min()
        {
            Assert.That(documents.Min(d => d.Scalar), Is.EqualTo(1));
        }

        [Test]
        public void Min_NoDocuments()
        {
            using (var session = provider.OpenSession<SampleDocument>())
            {
                session.DeleteAll();
            }
            
            Assert.That(() => documents.Min(d => d.Scalar), Throws.InvalidOperationException);
        }

    }
}