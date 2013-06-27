using System;
using System.Linq;
using Lucene.Net.Store;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class OrderByDateTimeTests : IntegrationTestBase
    {
        private static readonly DateTime time1 = new DateTime(2013, 6, 27, 18, 33, 22).ToUniversalTime();
        private static readonly DateTime time2 = time1.AddDays(-1);
        private static readonly DateTime time3 = time1.AddDays(1);

        public class Sample
        {
            public DateTime DateTime { get; set; }
        }

        [SetUp]
        public override void InitializeLucene()
        {
            directory = new RAMDirectory();
            provider = new LuceneDataProvider(directory, Net.Util.Version.LUCENE_30);

            AddDocument(new Sample {DateTime = time1});
            AddDocument(new Sample {DateTime = time2});
            AddDocument(new Sample {DateTime = time3});
        }

        [Test]
        public void OrderBy()
        {
            var documents = provider.AsQueryable<Sample>();

            var result = from d in documents orderby d.DateTime select d.DateTime;

            var sorted = new[] {time1, time2, time3}.OrderBy(t => t).ToArray();

            Assert.That(result.ToArray(), Is.EqualTo(sorted));
        }
    }
}