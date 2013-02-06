using System.IO;
using System.Text;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;

namespace Lucene.Net.Linq.Util
{
    internal static class AnalyzerExtensions
    {
        internal static string Analyze(this Analyzer analyzer, string fieldName, string pattern)
        {
            TokenStream s;

            try
            {
                s = analyzer.ReusableTokenStream(fieldName, new StringReader(pattern));
            }
            catch (IOException)
            {
                s = analyzer.TokenStream(fieldName, new StringReader(pattern));
            }

            var result = new StringBuilder();

            try
            {
                if (s.IncrementToken() && s.HasAttribute<ITermAttribute>())
                {
                    var attr = s.GetAttribute<ITermAttribute>();
                    result.Append(attr.Term);
                }
            }
            finally
            {
                s.Dispose();
            }

            return result.ToString();
        }
    }
}