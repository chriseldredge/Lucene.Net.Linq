using System;
using System.Linq;
using Lucene.Net.Search;

namespace Lucene.Net.Linq
{
    public interface ISession<T> : IDisposable
    {
        IQueryable<T> Query();
        void Add(params T[] items);
        /// <summary>
        /// Add items to the index allowing for specific key constraint behavior
        /// </summary>
        /// <param name="constraint"><see cref="KeyConstraint.Unique"/> is the default behavior, <see cref="KeyConstraint.None"/> will
        /// not perform a delete operation to ensure uniqueness</param>
        /// <param name="items"></param>
        void Add(KeyConstraint constraint, params T[] items);
        void Delete(params T[] items);
        void Delete(params Query[] items);
        void DeleteAll();
        void Commit();
        void Rollback();
    }
}
