using Lucene.Net.Analysis;
using Lucene.Net.Util;

namespace Lucene.Net.Linq
{
    internal class Context
    {
        private readonly Analyzer analyzer;
        private readonly Version version;

        public Context(Analyzer analyzer, Version version)
        {
            this.analyzer = analyzer;
            this.version = version;
        }

        public Analyzer Analyzer
        {
            get { return analyzer; }
        }

        public Version Version
        {
            get { return version; }
        }
    }
}