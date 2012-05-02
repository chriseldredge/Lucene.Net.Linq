using System;

namespace Lucene.Net.Linq.Util
{
    /// <summary>
    /// Controls which logging messages are emitted during execution.
    /// </summary>
    public static class Log
    {
#if DEBUG
        static Log()
        {
            TraceEnabled = true;
        }
#endif

        /// <summary>
        /// When set, messages will be written to <c cref="System.Diagnostics.Trace"/>
        /// to provide insight into how LINQ expressions are converted and
        /// what queries are being executed.
        /// </summary>
        public static bool TraceEnabled { get; set; }

        internal static void Trace(Func<string> format)
        {
            if (TraceEnabled)
            {
                System.Diagnostics.Trace.WriteLine(format(), "Lucene.Net.Linq");
            }
        }
    }
}