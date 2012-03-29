using System;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
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
        protected static readonly Version version = new Version("test", 0);

        [SetUp]
        public void SetUp()
        {
            directory = new RAMDirectory();
            writer = new IndexWriter(directory, new PorterStemAnalyzer(version), IndexWriter.MaxFieldLength.UNLIMITED);
            provider = new LuceneDataProvider(directory);
        }

        class PorterStemAnalyzer : StandardAnalyzer
        {
            public PorterStemAnalyzer(Version version) : base(version)
            {
            }

            public override TokenStream TokenStream(string fieldName, System.IO.TextReader reader)
            {
                return new PorterStemFilter(base.TokenStream(fieldName, reader));
            }
        }

        protected Document AddDocument(string id)
        {
            return AddDocument(id, null);
        }

        protected Document AddDocument(string id, string text)
        {
            var doc = new Document();
            
            doc.Add(new Field("id", id, Field.Store.YES, Field.Index.NOT_ANALYZED));

            if (text != null)
            {
                doc.Add(new Field("text", text, Field.Store.YES, Field.Index.ANALYZED));
            }

            writer.AddDocument(doc);
            writer.Commit();

            return doc;
        }
    }
}