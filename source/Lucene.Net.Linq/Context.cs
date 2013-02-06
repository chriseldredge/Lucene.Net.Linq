using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Lucene.Net.Search;
using Lucene.Net.Store;

namespace Lucene.Net.Linq
{
    internal class Context
    {
        private static readonly ILog Log = LogManager.GetLogger<Context>();

        public event EventHandler<SearcherLoadEventArgs> SearcherLoading;

        private readonly Directory directory;
        private readonly object transactionLock;

        private readonly object searcherLock = new object();
        private readonly object reloadLock = new object();
        private SearcherClientTracker tracker;
        
        public Context(Directory directory, object transactionLock)
        {
            this.directory = directory;
            this.transactionLock = transactionLock;
        }

        public Directory Directory
        {
            get { return directory; }
        }

        public object TransactionLock
        {
            get { return transactionLock; }
        }

        public ISearcherHandle CheckoutSearcher()
        {
            return new SearcherHandle(CurrentTracker);
        }

        public virtual void Reload()
        {
            Log.Info(m => m("Reloading index."));

            lock (reloadLock)
            {
                var newTracker = new SearcherClientTracker(CreateSearcher());

                var tmpHandler = SearcherLoading;

                if (tmpHandler != null)
                {
                    Log.Debug(m => m("Invoking SearcherLoading event."));
                    tmpHandler(this, new SearcherLoadEventArgs(newTracker.Searcher));
                }

                lock (searcherLock)
                {
                    if (tracker != null)
                    {
                        tracker.Dispose();
                    }

                    tracker = newTracker;
                }
            }

            Log.Debug(m => m("Index reloading completed."));
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

        protected virtual IndexSearcher CreateSearcher()
        {
            return new IndexSearcher(directory, true);
        }

        internal class SearcherHandle : ISearcherHandle
        {
            private readonly SearcherClientTracker tracker;
            private bool disposed;

            public SearcherHandle(SearcherClientTracker tracker)
            {
                this.tracker = tracker;
                tracker.AddClient(this);
            }

            public IndexSearcher Searcher
            {
                get { return tracker.Searcher; }
            }

            public void Dispose()
            {
                if (disposed) throw new ObjectDisposedException(typeof(ISearcherHandle).Name);
                disposed = true;
                tracker.RemoveClient(this);
            }
        }

        internal class SearcherClientTracker : IDisposable
        {
            private static readonly IList<SearcherClientTracker> undisposedTrackers = new List<SearcherClientTracker>();

            private readonly object sync = new object();
            private readonly List<WeakReference> searcherReferences = new List<WeakReference>();
            private readonly IndexSearcher searcher;
            private bool disposePending;
            private bool disposed;

            public SearcherClientTracker(IndexSearcher searcher)
            {
                this.searcher = searcher;

                lock(typeof(SearcherClientTracker))
                {
                    undisposedTrackers.Add(this);
                }
            }

            public IndexSearcher Searcher
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
                    searcherReferences.Remove(searcherReferences.First(wr => ReferenceEquals(wr.Target, client)));
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

            internal int ReferenceCount
            {
                get
                {
                    lock (sync) return searcherReferences.Count;
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
        IndexSearcher Searcher { get; }
    }

}