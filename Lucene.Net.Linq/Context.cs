using System;
using System.Collections.Generic;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq
{
    internal class Context
    {
        private readonly Directory directory;
        private readonly Analyzer analyzer;
        private readonly Version version;
        private readonly IIndexWriter indexWriter;
        private readonly object transactionLock;

        private readonly object searcherLock = new object();
        private SearcherClientTracker tracker;
        
        public Context(Directory directory, Analyzer analyzer, Version version, IIndexWriter indexWriter, object transactionLock)
        {
            this.directory = directory;
            this.analyzer = analyzer;
            this.version = version;
            this.indexWriter = indexWriter;
            this.transactionLock = transactionLock;
        }

        public Analyzer Analyzer
        {
            get { return analyzer; }
        }

        public Version Version
        {
            get { return version; }
        }

        public IIndexWriter IndexWriter
        {
            get { return indexWriter; }
        }

        public object TransactionLock
        {
            get { return transactionLock; }
        }

        public ISearcherHandle CheckoutSearcher(object client)
        {
            lock(searcherLock)
            {
                var current = CurrentTracker;
                current.AddClient(client);

                return new SearcherHandle(current, client);
            }
        }

        public virtual void Reload()
        {
            lock(searcherLock)
            {
                if (tracker != null)
                {
                    tracker.Dispose();
                }

                tracker = null;
            }
        }

        internal SearcherClientTracker CurrentTracker
        {
            get
            {
                lock (searcherLock)
                {
                    if (tracker == null)
                    {
                        tracker = new SearcherClientTracker(CreateSearcher());
                    }
                    return tracker;
                }
            }
        }

        public bool IsReadOnly
        {
            get { return IndexWriter ==  null; }
        }

        protected virtual IndexSearcher CreateSearcher()
        {
            return new IndexSearcher(directory, true);
        }

        internal class SearcherHandle : ISearcherHandle
        {
            private readonly SearcherClientTracker tracker;
            private readonly object client;

            public SearcherHandle(SearcherClientTracker tracker, object client)
            {
                this.tracker = tracker;
                this.client = client;
            }

            public Searcher Searcher
            {
                get { return tracker.Searcher; }
            }

            public void Dispose()
            {
                tracker.RemoveClient(client);
            }
        }

        internal class SearcherClientTracker : IDisposable
        {
            private static readonly IList<SearcherClientTracker> undisposedTrackers = new List<SearcherClientTracker>();

            private readonly object sync = new object();
            private readonly List<WeakReference> searcherReferences = new List<WeakReference>();
            private readonly Searcher searcher;
            private bool disposePending;
            private bool disposed;

            public SearcherClientTracker(Searcher searcher)
            {
                this.searcher = searcher;

                lock(typeof(SearcherClientTracker))
                {
                    undisposedTrackers.Add(this);
                }
            }

            public Searcher Searcher
            {
                get { return searcher; }
            }

            public void AddClient(object client)
            {
                lock (sync)
                    searcherReferences.Add(new WeakReference(client));
            }

            public void RemoveClient(object client)
            {
                lock (sync)
                {
                    searcherReferences.RemoveAll(wr => ReferenceEquals(wr.Target, client));
                    RemoveDeadReferences();

                    if (disposePending)
                    {
                        Dispose();
                    }
                }
            }

            public void Dispose()
            {
                lock (sync)
                {
                    disposePending = false;

                    if (disposed)
                    {
                        throw new ObjectDisposedException(GetType().Name);
                    }

                    RemoveDeadReferences();
                    if (searcherReferences.Count == 0)
                    {
                        lock (typeof(SearcherClientTracker))
                        {
                            undisposedTrackers.Remove(this);
                        }

                        searcher.Dispose();
                        disposed = true;
                    }
                    else
                    {
                        disposePending = true;
                    }
                }
            }

            private void RemoveDeadReferences()
            {
                searcherReferences.RemoveAll(wr => !wr.IsAlive);
            }
        }
       
    }

    internal interface ISearcherHandle : IDisposable
    {
        Searcher Searcher { get; }
    }

}