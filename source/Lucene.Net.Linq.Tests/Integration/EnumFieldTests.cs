using System.Linq;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class EnumFieldTests
    {
        private Directory directory;
        private LuceneDataProvider provider;

        public enum SampleEnum { Stuff, Things }

        public class Item
        {
            [Field(Key=true)]
            public string Id { get; set; }

            public SampleEnum Enum { get; set; }

            [NumericField]
            public SampleEnum NumericEnum { get; set; }
        }

        [SetUp]
        public void SetUp()
        {
            directory = new RAMDirectory();
            provider = new LuceneDataProvider(directory, Version.LUCENE_30);
            using (var session = provider.OpenSession<Item>())
            {
                session.Add(new Item { Id = "0", Enum = SampleEnum.Things, NumericEnum = SampleEnum.Things });
                session.Add(new Item { Id = "1", Enum = SampleEnum.Stuff, NumericEnum = SampleEnum.Stuff });
            }
        }

        [Test]
        public void EqualToEnum()
        {
            var count = provider.AsQueryable<Item>().Count(i => i.Enum == SampleEnum.Things);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void Greater()
        {
            var count = provider.AsQueryable<Item>().Count(i => i.Enum >= SampleEnum.Things);
            Assert.That(count, Is.EqualTo(1));
        }

        [Test]
        public void NotEqualToInt()
        {
            var count = provider.AsQueryable<Item>().Count(i => i.Enum != 0);
            Assert.That(count, Is.EqualTo(1));
        }
    }
}