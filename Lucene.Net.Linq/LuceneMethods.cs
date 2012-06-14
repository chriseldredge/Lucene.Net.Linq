using System;
using System.Linq;
using System.Linq.Expressions;
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

        ///<summary>
        /// Expression to be used in a LINQ where clauses to search
        /// for documents where any field matches a given pattern.
        ///</summary>
        public static string AnyField<T>(this T mappedDocument)
        {
            throw new InvalidOperationException(UnreachableCode);
        }

        /// <summary>
        /// Applies a boost to a property in a where clause.
        /// </summary>
        public static T Boost<T>(this T property, float boostAmount)
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
        /// Applies the provided Query. Enables queries to be constructed from outside of
        /// LINQ to be executed as part of a LINQ query.
        /// </summary>
        /// <returns></returns>
        public static IQueryable<T> Where<T>(this IQueryable<T> source, Query query)
        {
            return source.Where(i => Matches(query, i));
        }

        /// <summary>
        /// Expression to be used in a LINQ orderby clause to sort results by score.
        /// Note: since score is a decimal based weight, ordering by score normally
        /// results in additional orderby clauses having no effect.
        /// </summary>
        public static Expression Score<T>(this T mappedDocument)
        {
            throw new InvalidOperationException(UnreachableCode);
        }

        /// <summary>
        /// Instructs the query parser that a given query pattern
        /// in a LINQ where clause should not have special characters
        /// (such as <c>*</c>) escaped.
        /// 
        /// Disabling escaping allows prefix, wildcard, phrase and range queries
        /// to be parsed from the <paramref name="queryPattern"/> instead of
        /// treating it as a verbatim search term.
        /// 
        /// </summary>
        /// <example>
        /// The following two samples will produce the same <c cref="Query">Query</c>:
        ///     <c>
        ///         var query = "Foo*";
        /// 
        ///         var results = from doc in documents
        ///         where doc.Title == query.AllowSpecialCharacters()
        ///         select doc;
        ///     </c>
        ///     <c>
        ///         var query = "Foo";
        /// 
        ///         var results = from doc in documents
        ///         where doc.Title.StartsWith(query)
        ///         select doc;
        ///     </c>
        /// </example>
        /// <param name="queryPattern"></param>
        /// <returns></returns>
        public static string AllowSpecialCharacters(this string queryPattern)
        {
            throw new InvalidOperationException(UnreachableCode);
        }

        internal static bool Matches<T>(Query query, T item)
        {
            throw new InvalidOperationException(UnreachableCode);
        }
    }
}