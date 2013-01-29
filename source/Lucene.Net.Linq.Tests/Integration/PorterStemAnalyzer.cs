using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Util;

namespace Lucene.Net.Linq.Tests.Integration
{
    public class PorterStemAnalyzer : StandardAnalyzer
    {
        public PorterStemAnalyzer(Version version)
            : base(version)
        {
        }

        public override TokenStream TokenStream(string fieldName, System.IO.TextReader reader)
        {
            return new PorterStemFilter(base.TokenStream(fieldName, reader));
        }
    }
}