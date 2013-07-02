using System;
using System.Collections.Generic;
using System.Linq;
using Common.Logging;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Abstractions;
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
        private readonly IIndexWriter writer;
        private readonly Context context;
        private readonly IQueryable<T> queryable;

        private readonly List<T> additions = new List<T>();
        private readonly List<Query> deleteQueries = new List<Query>();
        private readonly ISet<IDocumentKey> deleteKeys = new HashSet<IDocumentKey>();

        private readonly SessionDocumentTracker documentTracker;

        public LuceneSession(IDocumentMapper<T> mapper, IIndexWriter writer, Context context, IQueryable<T> queryable)
        {
            this.mapper = mapper;
            this.writer = writer;
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
                var additions = StageModifiedDocuments();

                if (!PendingChanges)
                {
                    return;
                }

                lock (context.TransactionLock)
                {
                    try
                    {
                        CommitInternal(additions);
                    }
                    catch (OutOfMemoryException ex)
                    {
                        Log.Error(m => m("OutOfMemoryException while writing/committing to Lucene index. Closing writer."), ex);
                        try
                        {
                            writer.Dispose();
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
                            writer.Rollback();
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

        private void CommitInternal(IList<Document> additions)
        {
            var deletes = Deletions;

            if (DeleteAllFlag)
            {
                writer.DeleteAll();
            }
            else if (deletes.Any())
            {
                deletes.Apply(query => Log.Trace(m => m("Delete by query: " + query)));
                writer.DeleteDocuments(deletes.Distinct().ToArray());
            }

            if (additions.Count > 0)
            {
                additions.Apply(writer.AddDocument);
            }

            Log.Debug(m => m("Applied {0} deletes and {1} additions.", deletes.Count(), additions.Count));
            Log.Info(m => m("Committing."));

            writer.Commit();

            ClearPendingChanges();
            
            context.Reload();

            Log.Info(m => m("Commit completed."));
        }

        internal IList<Document> StageModifiedDocuments()
        {
            var docs = documentTracker.FindModifiedDocuments();
            foreach (var doc in docs)
            {
                var captured = doc;
                Log.Trace(m => m("Flushing modified document " + captured));

                if (!doc.Key.Empty)
                {
                    deleteKeys.Add(doc.Key);
                }

                Add(doc.Document);
            }

            var additions = ConvertPendingAdditions();

            Delete(additions.Keys.Where(k => !k.Empty).Select(k => k.ToQuery()).ToArray());

            return additions.Values.ToList();
        }

        internal IDictionary<IDocumentKey, Document> ConvertPendingAdditions()
        {
            var map = new Dictionary<IDocumentKey, Document>();
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
        
        internal IEnumerable<Query> Deletions { get { return deleteQueries.Union(deleteKeys.Select(k => k.ToQuery())); } }

        internal List<T> Additions
        {
            get { return additions; }
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
            private readonly IEnumerable<object> _originalFieldValues;
            private readonly IDocumentKey key;

            internal TrackedDocument(T document, IEnumerable<object> originalFieldValues, IDocumentKey key)
            {
                this.document = document;
                this._originalFieldValues = originalFieldValues;
                this.key = key;
            }

            internal T Document
            {
                get { return document; }
            }

            internal IEnumerable<object> OriginalFieldValues
            {
                get { return _originalFieldValues; }
            }

            internal IDocumentKey Key
            {
                get { return key; }
            }
        }

        internal class SessionDocumentTracker : IRetrievedDocumentTracker<T>
        {
            private readonly IDocumentMapper<T> mapper;
            private readonly IDictionary<IDocumentKey, T> byKey = new Dictionary<IDocumentKey, T>();
            private readonly ISet<IDocumentKey> deletedKeys = new HashSet<IDocumentKey>();
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

            public void TrackDocument(T item, IEnumerable<object> storedFieldValuesForChangeTracking)
            {
                var key = mapper.ToKey(item);
                byKey.Add(key, item);

                items.Add(new TrackedDocument(item, storedFieldValuesForChangeTracking, mapper.ToKey(item)));
            }

            public bool IsMarkedForDeletion(T item)
            {
                return deletedKeys.Contains(mapper.ToKey(item));
            }

            public void MarkForDeletion(IDocumentKey key)
            {
                deletedKeys.Add(key);
            }

            public IEnumerable<TrackedDocument> FindModifiedDocuments()
            {
                return items
                    .Where(t => !mapper.GetFieldValues(t.Document).SequenceEqual(t.OriginalFieldValues, new MapperComparer<T>(mapper))) //compare the new fields with the old ones
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

    internal class MapperComparer<T> : IEqualityComparer<object>
    {
        private readonly IDocumentMapper<T> _mapper;

        public MapperComparer(IDocumentMapper<T> mapper)
        {
            _mapper = mapper;
        }

        public bool Equals(object x, object y)
        {
            return _mapper.ValuesEquals(x, y);
        }

        public int GetHashCode(object obj)
        {
            throw new NotImplementedException();
        }
    }
}