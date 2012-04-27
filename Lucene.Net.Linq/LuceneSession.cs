using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Util;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    internal class LuceneSession<T> : ISession<T>
    {
        private readonly object sync = new object();
        private readonly IDocumentMapper<T> mapper;
        private readonly IIndexWriter writer;
        private readonly List<Document> additions = new List<Document>();
        private readonly List<Query> deletions = new List<Query>();

        internal LuceneSession(IDocumentMapper<T> mapper, IIndexWriter writer)
        {
            this.mapper = mapper;
            this.writer = writer;
        }

        public void Add(params T[] items)
        {
            var docs = items.Select(i =>
                                        {
                                            var doc = new Document();
                                            mapper.ToDocument(i, doc);
                                            return doc;
                                        });

            Add(docs.ToArray());
        }

        internal void Add(params Document[] docs)
        {
            lock (sync)
                additions.AddRange(docs);
        }

        public void Delete(params Query[] items)
        {
            lock (sync)
                deletions.AddRange(items);
        }

        public void DeleteAll()
        {
            DeleteAllFlag = true;
        }

        public void Commit()
        {
            lock (sync)
            {
                if (!PendingChanges)
                {
                    return;
                }

                try
                {
                    if (DeleteAllFlag)
                    {
                        writer.DeleteAll();
                    }
                    else if (deletions.Count > 0)
                    {
                        writer.DeleteDocuments(deletions.ToArray());
                    }

                    if (additions.Count > 0)
                    {
                        additions.Apply(writer.AddDocument);
                    }

                    writer.Commit();
                }
                catch (OutOfMemoryException)
                {
                    writer.Dispose();
                    throw;
                }
                catch(Exception)
                {
                    writer.Rollback();
                    throw;
                }

                ClearPendingChanges();
            }
        }

        private void ClearPendingChanges()
        {
            DeleteAllFlag = false;
            deletions.Clear();
            additions.Clear();
        }

        private bool PendingChanges
        {
            get { return DeleteAllFlag || additions.Count > 0 || deletions.Count > 0; }
        }

        public void Dispose()
        {
        }

        internal bool DeleteAllFlag { get; private set; }

        internal IEnumerable<Document> Additions { get { return new List<Document>(additions); } }
        internal IEnumerable<Query> Deletions { get { return new List<Query>(deletions); } }
    }
}