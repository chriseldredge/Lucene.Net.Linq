using System;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    internal class SearcherLoadEventArgs : EventArgs
    {
        private readonly IndexSearcher indexSearcher;

        public SearcherLoadEventArgs(IndexSearcher indexSearcher)
        {
            this.indexSearcher = indexSearcher;
        }

        public IndexSearcher IndexSearcher
        {
            get { return indexSearcher; }
        }
    }
}