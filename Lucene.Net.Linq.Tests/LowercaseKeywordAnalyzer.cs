using System.IO;
using Lucene.Net.Analysis;

namespace Lucene.Net.Linq.Tests
{
    public class LowercaseKeywordAnalyzer : KeywordAnalyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new LowerCaseFilter(base.TokenStream(fieldName, reader));
        }
    }
}