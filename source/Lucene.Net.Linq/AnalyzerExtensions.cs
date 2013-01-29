using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using Lucene.Net.Analysis;
using Remotion.Linq.Utilities;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Provides extensions to built in Lucene.Net Analyzer classes
    /// </summary>
    public static class AnalyzerExtensions
    {
        /// <summary>
        /// Defines an analyzer to use for the specified field in a strongly typed manner
        /// </summary>
        /// <typeparam name="TDocument">Type of the stored Lucene document</typeparam>
        /// <param name="this"></param>
        /// <param name="fieldName">field name requiring a non-default analyzer as a member expression</param>
        /// <param name="analyzer">non-default analyzer to use for field</param>
        public static void AddAnalyzer<TDocument>(this PerFieldAnalyzerWrapper @this, Expression<Func<TDocument, object>> fieldName, Analyzer analyzer)
        {
            try
            {
                @this.AddAnalyzer(((MemberExpression)fieldName.Body).Member.Name, analyzer);
            }
            catch (Exception ex)
            {
                throw new ArgumentException("Field name must be specified as a lambda to a member property, ex. doc => doc.FirstName", ex);
            }
        }
    }
}
