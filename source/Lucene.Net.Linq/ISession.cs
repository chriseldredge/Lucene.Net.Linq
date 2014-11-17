using System;
using System.Linq;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    public interface ISession<T> : IDisposable
    {
        IQueryable<T> Query();
        void Add(params T[] items);
        void AddWithoutDelete(params T[] items);
        void Delete(params T[] items);
        void Delete(params Query[] items);
        void DeleteAll();
        void Commit();
        void Rollback();
    }
}