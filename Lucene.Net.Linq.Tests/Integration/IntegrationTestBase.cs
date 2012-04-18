using System;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Store;
using NUnit.Framework;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Integration
{
    public abstract class IntegrationTestBase
    {
        protected LuceneDataProvider provider;
        protected Directory directory;
        protected IndexWriter writer;
        protected static readonly Version version = Version.LUCENE_29;

        [SetUp]
        public void SetUp()
        {
            directory = new RAMDirectory();
            writer = new IndexWriter(directory, GetAnalyzer(version), IndexWriter.MaxFieldLength.UNLIMITED);
            
            provider = new LuceneDataProvider(directory, writer.GetAnalyzer(), version);
        }

        protected virtual Analyzer GetAnalyzer(Version version)
        {
            return new PorterStemAnalyzer(version);
        }

        protected Document AddDocument(string id)
        {
            return AddDocument(id, null);
        }

        protected Document AddDocument(string id, string text)
        {
            var doc = new Document();
            
            doc.Add(new Field("id", id, Field.Store.YES, Field.Index.ANALYZED));

            if (text != null)
            {
                doc.Add(new Field("text", text, Field.Store.YES, Field.Index.ANALYZED));
            }

            AddDocument(doc);

            return doc;
        }

        protected void AddDocument(Document document)
        {
            writer.AddDocument(document);
            writer.Commit();
        }
    }
}