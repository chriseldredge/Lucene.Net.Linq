using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class LuceneDataProviderTests
    {
        public class Item
        {
            public int Id { get; set; }
        }

        [Test]
        public void OpenSessionThrowsWhenReadOnly()
        {
            var provider = new LuceneDataProvider(new RAMDirectory(), new SimpleAnalyzer(), Version.LUCENE_29);

            TestDelegate call = () => provider.OpenSession<Item>();

            Assert.That(call, Throws.InvalidOperationException);
        }

        [Test]
        public void RegisterCacheWarmingCallback()
        {
            var directory = new RAMDirectory();
            var writer = new IndexWriter(directory, new LowercaseKeywordAnalyzer(), true, IndexWriter.MaxFieldLength.UNLIMITED);
            var provider = new LuceneDataProvider(directory, new SimpleAnalyzer(), Version.LUCENE_29, writer);

            var count = -1;

            provider.RegisterCacheWarmingCallback<Item>(q => count = q.Count());

            provider.Context.Reload();

            Assert.That(count, Is.EqualTo(0));
        }
    }
}