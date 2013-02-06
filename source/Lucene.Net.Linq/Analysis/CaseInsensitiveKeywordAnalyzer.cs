using System.IO;
using Lucene.Net.Analysis;

namespace Lucene.Net.Linq.Analysis
{
    /// <summary>
    /// Decorates <see cref="KeywordAnalyzer"/> to convert the token stream
    /// to lowercase, allowing queries with different case-spelling to match.
    /// </summary>
    public class CaseInsensitiveKeywordAnalyzer : KeywordAnalyzer
    {
        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            return new LowerCaseFilter(base.TokenStream(fieldName, reader));
        }
    }
}
