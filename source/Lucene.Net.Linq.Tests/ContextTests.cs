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
        private TestableContext context;
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
            var handle = context.CheckoutSearcher();

            Assert.That(handle.Searcher, Is.Not.Null);
        }

        [Test]
        public void SearchHandleRetainsInstance()
        {
            var handle = context.CheckoutSearcher();

            var s1 = handle.Searcher;

            context.Reload();

            var s2 = handle.Searcher;
            Assert.That(s2, Is.SameAs(s1));
        }

        [Test]
        public void DisposeContextDisposesSearcher()
        {
            var searcher = context.CurrentTracker.Searcher;
            
            context.Dispose();

            searcher.AssertWasCalled(s => s.Dispose());
        }

        [Test]
        public void DisposeContextWaitsToDisposeSearcherWhenInUse()
        {
            var searcher = context.CurrentTracker.Searcher;

            using (context.CheckoutSearcher())
            {
                context.SimulateIndexReaderChanged();
                context.Reload();

                searcher.AssertWasNotCalled(s => s.Dispose());
            }

            searcher.AssertWasCalled(s => s.Dispose());
        }

        [Test]
        public void SearcherInstanceChangesOnReload()
        {
            var s1 = context.CheckoutSearcher().Searcher;
            context.SimulateIndexReaderChanged();

            context.Reload();

            var s2 = context.CheckoutSearcher().Searcher;
            Assert.That(s2, Is.Not.SameAs(s1), "Searcher instance after Reload()");
        }

        [Test]
        public void SearcherInstanceDoesNotChangeWhenIndexReaderNotReloaded()
        {
            var s1 = context.CheckoutSearcher().Searcher;

            context.Reload();

            var s2 = context.CheckoutSearcher().Searcher;

            Assert.That(s2, Is.SameAs(s1), "Searcher instance after Reload()");
        }

        [Test]
        public void DisposeHandleDoesNotDisposeSearcher()
        {
            var handle = context.CheckoutSearcher();

            var searcher = handle.Searcher;

            handle.Dispose();

            searcher.AssertWasNotCalled(s => s.Dispose());
        }

        [Test]
        public void ReloadDisposesSearcher()
        {
            var searcher = context.CurrentTracker.Searcher;
            context.SimulateIndexReaderChanged();

            context.Reload();

            searcher.AssertWasCalled(s => s.Dispose());
        }

        [Test]
        public void DisposeDisposesSearcher()
        {
            var searcher = context.CurrentTracker.Searcher;

            context.Dispose();

            searcher.AssertWasCalled(s => s.Dispose());
        }

        [Test]
        public void DisposeDisposesSearcherReader()
        {
            context.CurrentTracker.Searcher.Dispose();
            
            context.Dispose();

            context.FakeReader.AssertWasCalled(r => r.Dispose());
        }

        [Test]
        public void ReloadFiresLoadingEvent()
        {
            var searcher = context.CurrentTracker.Searcher;
            context.SimulateIndexReaderChanged();
            IndexSearcher current = null;
            IndexSearcher next = null;

            context.SearcherLoading += (e, x) => { current = context.CurrentTracker.Searcher; next = x.IndexSearcher; };
            context.Reload();

            Assert.That(current, Is.SameAs(searcher), "Should not have replaced current instance");
            Assert.That(next, Is.Not.Null, "Should create non-null new instance");
            Assert.That(next, Is.Not.SameAs(searcher), "Should create new instance");
        }

        [Test]
        public void ReloadDoesNotDisposeSearcherWhenInUse()
        {
            var searcher = context.CurrentTracker.Searcher;

            using (context.CheckoutSearcher())
            {
                context.Reload();

                searcher.AssertWasNotCalled(s => s.Dispose());
            }
        }

        [Test]
        public void DisposeHandleAfterReloadDisposesOldSearcher()
        {
            var searcher = context.CurrentTracker.Searcher;
            var handle = context.CheckoutSearcher();

            context.SimulateIndexReaderChanged();
            context.Reload();
            handle.Dispose();

            searcher.AssertWasCalled(s => s.Dispose());
        }

        [Test]
        public void DisposeHandleThrowsWhenAlreadyDisposed()
        {
            var handle = context.CheckoutSearcher();

            handle.Dispose();

            Assert.Throws<ObjectDisposedException>(handle.Dispose);
        }

        [Test]
        public void TwoHandles()
        {
            var h1 = context.CheckoutSearcher();
            var h2 = context.CheckoutSearcher();

            Assert.That(context.CurrentTracker.ReferenceCount, Is.EqualTo(2));

            h1.Dispose();

            Assert.That(context.CurrentTracker.ReferenceCount, Is.EqualTo(1));

            h2.Dispose();

            Assert.That(context.CurrentTracker.ReferenceCount, Is.EqualTo(0));
        }

        class TestableContext : Context
        {
            public IndexReader FakeReader { get; set; }

            public TestableContext(Directory directory, Analyzer analyzer, Version version, IIndexWriter indexWriter, object transactionLock)
                : base(directory, transactionLock)
            {
            }

            public void SimulateIndexReaderChanged()
            {
                FakeReader = MockRepository.GenerateMock<IndexReader>();
                FakeReader.Expect(r => r.Reopen()).WhenCalled(mi =>
                {
                    mi.ReturnValue = FakeReader;
                });
            }
            protected override IndexSearcher CreateSearcher()
            {
                SimulateIndexReaderChanged();
                
                var searcher = MockRepository.GenerateMock<IndexSearcher>(directory, true);
                searcher.Expect(s => s.IndexReader).Return(FakeReader);

                searcher.Expect(s => s.IndexReader).WhenCalled(mi =>
                {
                    mi.ReturnValue = FakeReader;
                });

                return searcher;
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

        public IndexReader GetReader()
        {
            return null;
        }

        public bool IsClosed
        {
            get
            {
                return false;
            }
        }
    }
}
