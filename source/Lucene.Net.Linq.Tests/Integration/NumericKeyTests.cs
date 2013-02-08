using System.Linq;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class NumericKeyTests
    {
        private Directory directory;
        private LuceneDataProvider provider;

        public class Item
        {
            [NumericField(Key = true)]
            public long Id { get; set; }

            public string Text { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            directory = new RAMDirectory();
            provider = new LuceneDataProvider(directory, Version.LUCENE_30);
        }

        [Test]
        public void StoreAndRetrieveById()
        {
            using (var session = provider.OpenSession<Item>())
            {
                session.Add(new Item { Id = 100 });
            }

            Assert.That(provider.AsQueryable<Item>().Count(), Is.EqualTo(1));
            Assert.That(provider.AsQueryable<Item>().First().Id, Is.EqualTo(100));
        }
    }
}