using Lucene.Net.Linq.Mapping;

namespace Lucene.Net.Linq
{
    /// <summary>
    /// Used in conjunction with <see cref="ISession{T}.Add(KeyConstraint,T[])"/>.
    /// </summary>
    public enum KeyConstraint
    {
        /// <summary>
        /// EXPERT: this constraint may be used by clients when the client
        /// knows that there is definitely not a document with the same key
        /// already present in the index.
        ///
        /// When <see cref="ISession{T}.Commit"/> is invoked, normally any
        /// pending updates and additions are preceding by deleting existing
        /// documents with the same <see cref="IDocumentKey"/> to ensure
        /// that the key is unique.
        ///
        /// This option is provided to improve performance by avoiding
        /// the delete-by-query step in the commit process.
        ///
        /// If this option is used incorrectly, the index may be left in
        /// an undesirable state where multiple documents have the same
        /// key. It should be used with caution.
        /// </summary>
        None,

        /// <summary>
        /// The default add behavior of the session <see cref="ISession{T}.Add(T[])"/>. Using this value
        /// on <see cref="ISession{T}.Add(KeyConstraint, T[])"/> has the same result of <see cref="ISession{T}.Add(T[])"/>.
        ///
        /// In this mode, invoking <see cref="ISession{T}.Commit"/> will execute
        /// a delete query for each unique <see cref="IDocumentKey"/> that
        /// corresponds to an updated or added item.
        /// </summary>
        Unique
    }
}
