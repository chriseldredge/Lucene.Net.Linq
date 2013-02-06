using System;
using System.Collections.Generic;
using System.IO;
using Lucene.Net.Analysis;

namespace Lucene.Net.Linq.Analysis
{
    /// <summary>
    /// Similar to <see cref="PerFieldAnalyzerWrapper"/> but
    /// prevents collisions of different analyzers being
    /// added for the same field.
    /// </summary>
    public class PerFieldAnalyzer : Analyzer
    {
        private readonly Analyzer defaultAnalyzer;
        private readonly IDictionary<string, Analyzer> analyzerMap = new Dictionary<string, Analyzer>();
        
        /// <summary> Constructs with default analyzer.
        /// 
        /// </summary>
        /// <param name="defaultAnalyzer">Any fields not specifically
        /// defined to use a different analyzer will use the one provided here.
        /// </param>
        public PerFieldAnalyzer(Analyzer defaultAnalyzer)
        {
            this.defaultAnalyzer = defaultAnalyzer;
        }

        /// <summary> Defines an analyzer to use for the specified field.
        /// 
        /// </summary>
        /// <param name="fieldName">field name requiring a non-default analyzer
        /// </param>
        /// <param name="analyzer">non-default analyzer to use for field
        /// </param>
        public virtual void AddAnalyzer(string fieldName, Analyzer analyzer)
        {
            lock (analyzerMap)
            {
                Analyzer previous;
                if (analyzerMap.TryGetValue(fieldName, out previous) && previous.GetType() != analyzer.GetType())
                {
                    throw new InvalidOperationException(string.Format("Attempt to replace analyzer for field {0} with analyzer of type {1}. Analyzer type {2} is already in use.", fieldName, previous.GetType(), analyzer.GetType()));
                }
                analyzerMap[fieldName] = analyzer;
            }
        }

        public override TokenStream TokenStream(string fieldName, TextReader reader)
        {
            var analyzer = GetAnalyzerForField(fieldName);

            return analyzer.TokenStream(fieldName, reader);
        }

        public override TokenStream ReusableTokenStream(string fieldName, TextReader reader)
        {
#pragma warning disable 612,618
            if (overridesTokenStreamMethod)
#pragma warning restore 612,618
            {
                // LUCENE-1678: force fallback to tokenStream() if we
                // have been subclassed and that subclass overrides
                // tokenStream but not reusableTokenStream
                return TokenStream(fieldName, reader);
            }

            var analyzer = GetAnalyzerForField(fieldName);

            return analyzer.ReusableTokenStream(fieldName, reader);
        }

        /// <summary>Return the positionIncrementGap from the analyzer assigned to fieldName </summary>
        public override int GetPositionIncrementGap(string fieldName)
        {
            return GetAnalyzerForField(fieldName).GetPositionIncrementGap(fieldName);
        }

        public override string ToString()
        {
            return "PerFieldAnalyzerWrapper(" + analyzerMap + ", default=" + defaultAnalyzer + ")";
        }
        
        /// <summary>
        /// Copy field analyzers from another instance into this instance.
        /// </summary>
        /// <param name="other"></param>
        public virtual void Merge(PerFieldAnalyzer other)
        {
            foreach (var kv in other.analyzerMap)
            {
                AddAnalyzer(kv.Key, kv.Value);
            }
        }

        public virtual Analyzer this[string fieldName]
        {
            get { return GetAnalyzerForField(fieldName); }
        }

        protected virtual Analyzer GetAnalyzerForField(string fieldName)
        {
            Analyzer analyzer;

            lock(analyzerMap)
            {
                if (!analyzerMap.TryGetValue(fieldName, out analyzer))
                {
                    analyzer = defaultAnalyzer;
                }
            }
            return analyzer;
        }
    }
}