using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Lucene.Net.Linq.Fluent;
using Lucene.Net.Util;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class EnumerableContainsTests : IntegrationTestBase
    {
        private ClassMap<Tour> tourMap;

        public class Tour
        {
            public IEnumerable<int> AccommodationAges { get; set; }
            public decimal TotalPriceMin { get; set; }
            public decimal TotalPriceMax { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            tourMap = new ClassMap<Tour>(Version.LUCENE_30);
            tourMap.Property(p => p.AccommodationAges).Stored().NotAnalyzed();
            tourMap.Property(p => p.TotalPriceMin).Stored().NotAnalyzed();
            tourMap.Property(p => p.TotalPriceMax).Stored().NotAnalyzed();
        }

        [Test]
        public void ContainsInt()
        {
            using (var session = provider.OpenSession(tourMap.ToDocumentMapper()))
            {
                session.Add(new Tour { AccommodationAges = new[] {10, 12}, TotalPriceMax = 100 });
                session.Add(new Tour { AccommodationAges = new[] {5, 12}, TotalPriceMax = 90 });
            }

            using (var session = provider.OpenSession(tourMap.ToDocumentMapper()))
            {
                var q = session.Query().Where(t => t.AccommodationAges.Contains(10));
                var result = q.ToList();
                Assert.That(result.Count, Is.EqualTo(1), "Expected single result");
                Assert.That(result.Single().TotalPriceMax, Is.EqualTo(100));
            }
        }
    }
}
