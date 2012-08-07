using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    internal class LuceneSession<T> : ISession<T>
    {
        private readonly ILog Log = LogManager.GetCurrentClassLogger();

        private readonly object sessionLock = new object();
        
        private readonly IDocumentMapper<T> mapper;
        private readonly Context context;
        private readonly IQueryable<T> queryable;

        private readonly List<T> additions = new List<T>();
        private readonly List<Query> deleteQueries = new List<Query>();
        private readonly ISet<DocumentKey> deleteKeys = new HashSet<DocumentKey>();

        private readonly SessionDocumentTracker documentTracker;

        public LuceneSession(IDocumentMapper<T> mapper, Context context, IQueryable<T> queryable)
        {
            this.mapper = mapper;
            this.context = context;
            this.queryable = queryable;
            documentTracker = new SessionDocumentTracker(mapper);
        }

        public IQueryable<T> Query()
        {
            return queryable.TrackRetrievedDocuments(documentTracker);
        }

        public void Add(params T[] items)
        {
            Add((IEnumerable<T>) items);
        }

        public void Add(IEnumerable<T> items)
        {
            lock (sessionLock)
            {
                additions.AddRange(items);
            }
        }

        public void Delete(params T[] items)
        {
            lock (sessionLock)
            {
                foreach (var item in items)
                {
                    additions.Remove(item);
                    var key = mapper.ToKey(item);
                    if (key.Empty)
                    {
                        throw new InvalidOperationException("The type " + typeof(T) + " does not specify any key fields.");
                    }
                    deleteKeys.Add(key);
                }
            }
        }

        public void Delete(params Query[] items)
        {
            lock (sessionLock)
                deleteQueries.AddRange(items);
        }

        public void DeleteAll()
        {
            lock (sessionLock)
            {
                DeleteAllFlag = true;
                additions.Clear();
            }
        }

        public void Dispose()
        {
            Commit();
        }

        public void Rollback()
        {
            lock (sessionLock)
            {
                ClearPendingChanges();
            }
        }

        public void Commit()
        {
            lock (sessionLock)
            {
                StageModifiedDocuments();

                if (!PendingChanges)
                {
                    return;
                }

                lock (context.TransactionLock)
                {
                    try
                    {
                        CommitInternal();
                    }
                    catch (OutOfMemoryException ex)
                    {
                        Log.Error(m => m("OutOfMemoryException while writing/committing to Lucene index. Closing writer."), ex);
                        try
                        {
                            context.IndexWriter.Dispose();
                        }
                        catch (Exception ex2)
                        {
                            Log.Error(m => m("Exception in IndexWriter.Dispose"), ex2);
                            throw new AggregateException(ex, ex2);
                        }

                        throw;
                    }
                    catch (Exception ex)
                    {
                        Log.Error(m => m("Exception in commit"), ex);
                        try
                        {
                            context.IndexWriter.Rollback();
                        }
                        catch (Exception ex2)
                        {
                            Log.Error(m => m("Exception in rollback"), ex2);
                            throw new AggregateException(ex, ex2);
                        }
                        
                        throw;
                    }
                }
            }
        }

        private void CommitInternal()
        {
            var writer = context.IndexWriter;
            IEnumerable<Query> deletes = new Query[0];

            if (DeleteAllFlag)
            {
                writer.DeleteAll();
            }
            else if (deleteQueries.Count > 0 || deleteKeys.Count > 0)
            {
                deletes = Deletions;
            }

            var additionMap = ConvertPendingAdditions();

            deletes = deletes.Union(additionMap.Keys.Where(k => !k.Empty).Select(k => k.ToQuery(context.Analyzer, context.Version)));

            if (deletes.Any())
            {
                writer.DeleteDocuments(deletes.Distinct().ToArray());
            }

            if (additionMap.Count > 0)
            {
                additionMap.Values.Apply(writer.AddDocument);
            }

            writer.Commit();

            ClearPendingChanges();

            context.Reload();
        }

        internal void StageModifiedDocuments()
        {
            var docs = documentTracker.FindModifiedDocuments();
            foreach (var doc in docs)
            {
                Log.Debug(m => m("Flushing modified document " + doc));
                Add(doc);
            }
        }

        private void ClearPendingChanges()
        {
            DeleteAllFlag = false;
            deleteKeys.Clear();
            deleteQueries.Clear();
            additions.Clear();
            documentTracker.Clear();
        }

        internal bool PendingChanges
        {
            get { return DeleteAllFlag || additions.Count > 0 || deleteQueries.Count > 0 || deleteKeys.Count > 0; }
        }

        internal IRetrievedDocumentTracker<T> DocumentTracker { get { return documentTracker; } }

        internal bool DeleteAllFlag { get; private set; }
        
        internal IEnumerable<Query> Deletions { get { return deleteQueries.Union(deleteKeys.Select(k => k.ToQuery(context.Analyzer, context.Version))); } }

        internal List<T> Additions
        {
            get { return additions; }
        }

        internal IDictionary<DocumentKey, Document> ConvertPendingAdditions()
        {
            var map = new Dictionary<DocumentKey, Document>();
            var reverse = new List<T>(additions);
            reverse.Reverse();

            foreach (var item in reverse)
            {
                var key = mapper.ToKey(item);
                if (!map.ContainsKey(key))
                {
                    map[key] = ToDocument(item);
                }
            }

            return map;
        }

        private Document ToDocument(T i)
        {
            var doc = new Document();
            mapper.ToDocument(i, doc);
            return doc;
        }

        internal class SessionDocumentTracker : IRetrievedDocumentTracker<T>
        {
            private readonly IDocumentMapper<T> mapper;
            private readonly IList<Tuple<T, T>> items = new List<Tuple<T, T>>();

            public SessionDocumentTracker(IDocumentMapper<T> mapper)
            {
                this.mapper = mapper;
            }

            public void TrackDocument(T item, T hiddenCopy)
            {
                items.Add(new Tuple<T, T>(item, hiddenCopy));
            }

            public IEnumerable<T> FindModifiedDocuments()
            {
                return items.Where(t => !mapper.Equals(t.Item1, t.Item2)).Select(t => t.Item1);
            }

            public void Clear()
            {
                items.Clear();
            }
        }
    }
}