using System.Linq;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class DocumentHolderMultiValueTests
    {
        public class MappedDocument : DocumentHolder
        {
            public string[] Names
            {
                get { return GetValues("Names").ToArray(); }
                set { Set("Names", value, Field.Store.YES, Field.Index.ANALYZED); }
            }
        }

        [Test]
        public void SetAddsMultipleFields()
        {
            var doc = new MappedDocument();

            var strings = new[] {"a", "b", "c"};

            doc.Names = strings;

            Assert.That(doc.Document.GetValues("Names"), Is.EqualTo(strings));
        }

        [Test]
        public void GetMutliValues()
        {
            var doc = new MappedDocument();

            var document = doc.Document;

            document.Add(new Field("Names", "apple", Field.Store.YES, Field.Index.NO));
            document.Add(new Field("Names", "banana", Field.Store.YES, Field.Index.NO));
            
            Assert.That(doc.Names, Is.EqualTo(new[] {"apple", "banana"}));
        }
    }
}