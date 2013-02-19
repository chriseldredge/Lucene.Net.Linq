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

        [Test]
        public void DeleteByNumericId()
        {
            using (var session = provider.OpenSession<Item>())
            {
                session.Add(new Item { Id = 5501 });
                session.Add(new Item { Id = 5502 });
            }

            using (var session = provider.OpenSession<Item>())
            {
                session.Delete(session.Query().OrderBy(i => i.Id).First());
            }

            Assert.That(provider.AsQueryable<Item>().Single().Id, Is.EqualTo(5502));
        }

        [Test]
        public void QueryById()
        {
            using (var session = provider.OpenSession<Item>())
            {
                session.Add(new Item { Id = 5501 });
                session.Add(new Item { Id = 5502 });
            }
            
            var item = provider.AsQueryable<Item>().Where(i => i.Id == 5501);
            
            Assert.That(item.Single().Id, Is.EqualTo(5501));
        }
    }
}