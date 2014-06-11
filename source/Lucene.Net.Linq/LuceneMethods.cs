using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Search;

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
        /// multiplied to yield a final result. Warning: this method will cause each document that
        /// matches the query to be converted to an instance of <typeparamref name="T"/> in order
        /// for the score to be computed, significantly degrading performance.
        /// </summary>
        public static IQueryable<T> Boost<T>(this IQueryable<T> source, Func<T, float> boostFunction)
        {
            Expression<Func<T, float>> func = t => boostFunction(t);
            return source.BoostInternal(func);
        }

        /// <summary>
        /// Applies a custom boost function to customize query scoring. When multiple boost functions
        /// are added by calling this method more than once, the return values from each function are
        /// multiplied to yield a final result.
        /// </summary>
        internal static IQueryable<T> BoostInternal<T>(this IQueryable<T> source, Expression<Func<T, float>> boostFunction)
        {
            return source.Provider.CreateQuery<T>(
                Expression.Call(((MethodInfo) MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof (T)),
                                source.Expression, boostFunction));
        }

        public static bool Fuzzy(this bool predicate, float similarity)
        {
            throw new InvalidOperationException(UnreachableCode);
        }

        internal static IQueryable<T> TrackRetrievedDocuments<T>(this IQueryable<T> source, IRetrievedDocumentTracker<T> tracker)
        {
            return source.Provider.CreateQuery<T>(
                Expression.Call(((MethodInfo)MethodBase.GetCurrentMethod()).MakeGenericMethod(typeof(T)),
                                source.Expression, Expression.Constant(tracker)));
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
        public static T AllowSpecialCharacters<T>(this T queryPattern)
        {
            throw new InvalidOperationException(UnreachableCode);
        }

        internal static bool Matches<T>(Query query, T item)
        {
            throw new InvalidOperationException(UnreachableCode);
        }
    }
}