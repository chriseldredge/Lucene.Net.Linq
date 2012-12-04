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
                    DocumentTracker.MarkForDeletion(key);
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

            Log.Trace(m => m("Applied {0} deletes and {1} additions.", deletes.Count(), additionMap.Count));
            Log.Info(m => m("Committing."));

            writer.Commit();

            ClearPendingChanges();
            
            context.Reload();

            Log.Info(m => m("Commit completed."));
        }

        internal void StageModifiedDocuments()
        {
            var docs = documentTracker.FindModifiedDocuments();
            foreach (var doc in docs)
            {
                Log.Trace(m => m("Flushing modified document " + doc));

                if (!doc.Key.Empty)
                {
                    deleteKeys.Add(doc.Key);
                }

                Add(doc.Document);
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

        internal SessionDocumentTracker DocumentTracker { get { return documentTracker; } }

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

        internal class TrackedDocument
        {
            private readonly T document;
            private readonly T hiddenCopy;
            private readonly DocumentKey key;

            internal TrackedDocument(T document, T hiddenCopy, DocumentKey key)
            {
                this.document = document;
                this.hiddenCopy = hiddenCopy;
                this.key = key;
            }

            internal T Document
            {
                get { return document; }
            }

            internal T HiddenCopy
            {
                get { return hiddenCopy; }
            }

            internal DocumentKey Key
            {
                get { return key; }
            }
        }

        internal class SessionDocumentTracker : IRetrievedDocumentTracker<T>
        {
            private readonly IDocumentMapper<T> mapper;
            private readonly IDictionary<DocumentKey, T> byKey = new Dictionary<DocumentKey, T>();
            private readonly ISet<DocumentKey> deletedKeys = new HashSet<DocumentKey>();
            private readonly IList<TrackedDocument> items = new List<TrackedDocument>();

            public SessionDocumentTracker(IDocumentMapper<T> mapper)
            {
                this.mapper = mapper;
            }

            public bool TryGetTrackedDocument(T item, out T tracked)
            {
                tracked = default(T);

                var key = mapper.ToKey(item);
                if (key.Empty) return false;
                
                return byKey.TryGetValue(key, out tracked);
            }

            public void TrackDocument(T item, T hiddenCopy)
            {
                var key = mapper.ToKey(item);
                byKey.Add(key, item);

                items.Add(new TrackedDocument(item, hiddenCopy, mapper.ToKey(hiddenCopy)));
            }

            public bool IsMarkedForDeletion(T item)
            {
                return deletedKeys.Contains(mapper.ToKey(item));
            }

            public void MarkForDeletion(DocumentKey key)
            {
                deletedKeys.Add(key);
            }

            public IEnumerable<TrackedDocument> FindModifiedDocuments()
            {
                return items
                    .Where(t => !mapper.Equals(t.Document, t.HiddenCopy))
                    .Where(t => !IsMarkedForDeletion(t.Document));
            }

            public void Clear()
            {
                byKey.Clear();
                items.Clear();
                deletedKeys.Clear();
            }
        }
    }
}