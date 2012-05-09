using Lucene.Net.Analysis;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class LuceneDataProviderTests
    {
        [Test]
        public void OpenSessionThrowsWhenReadOnly()
        {
            var provider = new LuceneDataProvider(new RAMDirectory(), new SimpleAnalyzer(), Version.LUCENE_29);

            TestDelegate call = () => provider.OpenSession<object>();

            Assert.That(call, Throws.InvalidOperationException);
        }
    }
}