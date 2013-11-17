using System;
using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class RangeTests : IntegrationTestBase
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
        public void Combined()
        {
            var result = from d in documents
                         where
                            d.Scalar >= 1
                         && d.Scalar < 3
                         select d;

            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public void Inverse()
        {
            var result = from d in documents
                         where
                            d.Scalar < 3
                         && d.Scalar >= 1
                         select d;

            Assert.That(result.Count(), Is.EqualTo(2));
        }

        [Test]
        public void ExcludeRange()
        {
            var result = from d in documents
                         where 
                            d.Scalar < 2
                         || d.Scalar >= GetThree()
                         select d.Scalar;

            Assert.That(result.ToList(), Is.EquivalentTo(new[] {1, 3}));
        }

        private static int GetThree()
        {
            return 3;
        }
    }
}