using System;
using NUnit.Framework;
using Lucene.Net.Index;
using Lucene.Net.Store;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Analysis;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class IndexWriterAdapterTests
    {
        [Test]
        public void SetsFlagOnDispose()
        {
            var target = new IndexWriter(new RAMDirectory(), new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);

            var adapter = new IndexWriterAdapter(target);

            adapter.Dispose();

            Assert.That(adapter.IsClosed, Is.True, "Should set flag on Dispose");
        }

        [Test]
        public void SetsFlagOnRollback()
        {
            var target = new IndexWriter(new RAMDirectory(), new KeywordAnalyzer(), IndexWriter.MaxFieldLength.UNLIMITED);
            
            var adapter = new IndexWriterAdapter(target);
            
            adapter.Rollback();
            
            Assert.That(adapter.IsClosed, Is.True, "Should set flag on Dispose");
        }
    }
}

