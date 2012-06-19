using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    internal class LuceneSession<T> : ISession<T>
    {
        private readonly object sessionLock = new object();
        
        private readonly IDocumentMapper<T> mapper;
        private readonly Context context;
        
        private readonly IDictionary<DocumentKey, Document> additions = new Dictionary<DocumentKey, Document>();
        private readonly List<Query> deleteQueries = new List<Query>();
        private readonly ISet<DocumentKey> deleteKeys = new HashSet<DocumentKey>();

        public LuceneSession(IDocumentMapper<T> mapper, Context context)
        {
            this.mapper = mapper;
            this.context = context;
        }

        public void Add(params T[] items)
        {
            lock (sessionLock)
            {
                foreach (var item in items)
                {
                    var key = mapper.ToKey(item);
                    var doc = new Document();

                    mapper.ToDocument(item, doc);
                    
                    additions[key] = doc;
                }
            }
        }

        internal void Add(DocumentKey key, Document doc)
        {
            additions[key] = doc;
        }

        public void Delete(params T[] items)
        {
            lock (sessionLock)
            {
                foreach (var item in items)
                {
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

        public void Commit()
        {
            lock (sessionLock)
            {
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
                    catch (OutOfMemoryException)
                    {
                        context.IndexWriter.Dispose();
                        throw;
                    }
                    catch (Exception)
                    {
                        context.IndexWriter.Rollback();
                        throw;
                    }
                }
            }
        }

        public void Dispose()
        {
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

            deletes = deletes.Union(additions.Keys.Where(k => !k.Empty).Select(k => k.ToQuery(context.Analyzer, context.Version)));

            if (deletes.Any())
            {
                writer.DeleteDocuments(deletes.Distinct().ToArray());
            }

            if (additions.Count > 0)
            {
                additions.Values.Apply(writer.AddDocument);
            }

            writer.Commit();

            ClearPendingChanges();

            context.Reload();
        }

        private void ClearPendingChanges()
        {
            DeleteAllFlag = false;
            deleteKeys.Clear();
            deleteQueries.Clear();
            additions.Clear();
        }

        internal bool PendingChanges
        {
            get { return DeleteAllFlag || additions.Count > 0 || deleteQueries.Count > 0 || deleteKeys.Count > 0; }
        }

        internal bool DeleteAllFlag { get; private set; }

        internal IEnumerable<Document> Additions { get { return new List<Document>(additions.Values); } }
        internal IEnumerable<Query> Deletions { get { return deleteQueries.Union(deleteKeys.Select(k => k.ToQuery(context.Analyzer, context.Version))); } }
    }
}