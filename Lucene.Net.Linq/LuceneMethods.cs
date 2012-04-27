using System;
using System.Linq.Expressions;
using Lucene.Net.Linq.Expressions;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Contains custom extensions to LINQ for integrating with Lucene.Net.
    /// </summary>
    public static class LuceneMethods
    {
        /// <summary>
        /// Expression to be used in orderby clauses to sort results by score.
        /// Since score is a decimal based weight, ordering by score normally
        /// results in secondary orderby clauses to have no effect.
        /// </summary>
        public static Expression Score<T>(this T mappedDocument)
        {
            return LuceneOrderByRelevanceExpression.Instance;
        }
    }
}