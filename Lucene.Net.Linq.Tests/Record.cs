using Lucene.Net.Linq.Mapping;

namespace Lucene.Net.Linq.Tests
{
    public class Record
    {
        public string Name { get; set; }

        [Field(Key = true)]
        public string Id { get; set; }
    }
}