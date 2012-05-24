using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Linq.Expressions;
using Lucene.Net.Search;
using Remotion.Linq;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Contains custom extensions to LINQ for integrating with Lucene.Net.
    /// </summary>
    public static class LuceneMethods
    {
        private const string UnreachableCode = "Unreachable code. This method should have been translated within a LINQ expression and not directly invoked.";

        /// <summary>
        /// Expression to be used in orderby clauses to sort results by score.
        /// Note: since score is a decimal based weight, ordering by score normally
        /// results in additional orderby clauses having no effect.
        /// </summary>
        public static Expression Score<T>(this T mappedDocument)
        {
            throw new InvalidOperationException(UnreachableCode);
        }

        ///<summary>
        /// Expression to be used in where clauses to search
        /// for documents where any field matches a given pattern.
        ///</summary>
        public static string AnyField<T>(this T mappedDocument)
        {
            throw new InvalidOperationException(UnreachableCode);
        }

        /// <summary>
        /// Applies a custom boost function to customize query scoring. When multiple boost functions
        /// are added by calling this method more than once, the return values from each function are
        /// multiplied to yield a final result.
        /// </summary>
        public static IQueryable<T> Boost<T>(this IQueryable<T> source, Func<T, float> boostFunction)
        {
            var provider = (QueryProviderBase) source.Provider;
            var executor = (LuceneQueryExecutor<T>)provider.Executor;

            executor.AddCustomScoreFunction(boostFunction);

            return source;
        }

        /// <summary>
        /// Applies a boost to a property in a where clause.
        /// </summary>
        public static T Boost<T>(this T property, float boostAmount)
        {
            throw new InvalidOperationException(UnreachableCode);
        }
    }
}