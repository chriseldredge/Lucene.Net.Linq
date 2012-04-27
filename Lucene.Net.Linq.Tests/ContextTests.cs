using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using NUnit.Framework;
using Rhino.Mocks;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class ContextTests
    {
        private Context context;
        private static readonly Directory directory = new RAMDirectory();

        [SetUp]
        public void SetUp()
        {
            var analyzer = new StandardAnalyzer(Version.LUCENE_29);
            context = new TestableContext(directory, analyzer, Version.LUCENE_29, new NoOpIndexWriter(), new object());

            var writer = new IndexWriter(directory, analyzer, true, IndexWriter.MaxFieldLength.UNLIMITED);

            writer.Commit();
        }

        [Test]
        public void SearchHandleCreatesNew()
        {
            var handle = context.CheckoutSearcher(this);

            Assert.That(handle.Searcher, Is.Not.Null);
        }

        [Test]
        public void SearchHandleRetainsInstance()
        {
            var handle = context.CheckoutSearcher(this);

            var s1 = handle.Searcher;

            context.Reload();

            var s2 = handle.Searcher;
            Assert.That(s2, Is.SameAs(s1));
        }

        [Test]
        public void SearcherInstanceChangesOnReload()
        {
            var s1 = context.CheckoutSearcher(this).Searcher;

            context.Reload();

            var s2 = context.CheckoutSearcher(this).Searcher;

            Assert.That(s2, Is.Not.SameAs(s1), "Searcher instance after Reload()");
        }

        [Test]
        public void DisposeHandleDoesNotDisposesSearcher()
        {
            var handle = context.CheckoutSearcher(this);

            var searcher = handle.Searcher;

            handle.Dispose();

            searcher.AssertWasNotCalled(s => s.Dispose());
        }

        [Test]
        public void ReloadDisposesSearcher()
        {
            var searcher = context.CurrentTracker.Searcher;

            context.Reload();

            searcher.AssertWasCalled(s => s.Dispose());
        }

        [Test]
        public void ReloadDoesNotDisposeSearcherWhenInUse()
        {
            var searcher = context.CurrentTracker.Searcher;

            context.CheckoutSearcher(this);

            context.Reload();

            searcher.AssertWasNotCalled(s => s.Dispose());
        }

        [Test]
        public void DisposeHandleAfterReloadDisposesOldSearcher()
        {
            var searcher = context.CurrentTracker.Searcher;

            var handle = context.CheckoutSearcher(this);

            context.Reload();

            handle.Dispose();

            searcher.AssertWasCalled(s => s.Dispose());
        }

        class TestableContext : Context
        {
            public TestableContext(Directory directory, Analyzer analyzer, Version version, IIndexWriter indexWriter, object transactionLock)
                : base(directory, analyzer, version, indexWriter, transactionLock)
            {
            }

            protected override IndexSearcher CreateSearcher()
            {
                return MockRepository.GenerateMock<IndexSearcher>(directory, true);
            }
        }
    }

    public class NoOpIndexWriter : IIndexWriter
    {
        public void Dispose()
        {
        }

        public void AddDocument(Document doc)
        {
        }

        public void DeleteDocuments(Query[] queries)
        {
        }

        public void DeleteAll()
        {
        }

        public void Commit()
        {
        }

        public void Rollback()
        {
        }

        public void Optimize()
        {
        }
    }
}