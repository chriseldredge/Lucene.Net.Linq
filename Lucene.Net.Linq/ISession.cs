using System;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    public interface ISession<in T> : IDisposable
    {
        void Add(params T[] items);
        void Delete(params T[] items);
        void Delete(params Query[] items);
        void DeleteAll();
        void Commit();
    }
}