using Lucene.Net.Documents;

namespace Lucene.Net.Linq
{
    public interface IDocumentHolder
    {
        Document Document { get; set; }
    }
}