using System;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Abstractions;
using Lucene.Net.Store;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq
{
    public class ReadOnlyLuceneDataProvider : LuceneDataProvider
    {
        public ReadOnlyLuceneDataProvider(Directory directory, Version version) : base(directory, version)
        {
        }

        public ReadOnlyLuceneDataProvider(Directory directory, Analyzer externalAnalyzer, Version version) : base(directory, externalAnalyzer, version)
        {
        }

        protected override IIndexWriter GetIndexWriter(Analyzer analyzer)
        {
            return null;
        }

        public override ISession<T> OpenSession<T>(Func<T> factory, Mapping.IDocumentMapper<T> documentMapper, Mapping.IDocumentModificationDetector<T> documentModificationDetector)
        {
            throw new InvalidOperationException("Cannot open sessions in read-only mode.");
        }
    }
}