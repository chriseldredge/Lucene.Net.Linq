using System.Linq;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    
    [TestFixture]
    public class DocumentHolderTests : IntegrationTestBase
    {
        public class MappedDocument : DocumentHolder
        {
            public string Name
            {
                get { return Get("Name"); }
                set { Set("Name", value); }
            }

            public int? Scalar
            {
                get { return GetNumeric<int>("Scalar"); }
                set { SetNumeric("Scalar", value); }
            }
        }

        [Test]
        public void Select()
        {
            var d = new MappedDocument {Name = "My Document"};
            AddDocument(d.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents select doc;

            Assert.That(result.FirstOrDefault().Name, Is.EqualTo(d.Name));
        }

        [Test]
        public void SelectScalar()
        {
            const int scalar = 99;

            var d = new MappedDocument {Name = "a", Scalar = scalar};

            AddDocument(d.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents select doc.Scalar;

            Assert.That(result.FirstOrDefault(), Is.EqualTo(scalar));
        }

        [Test]
        public void Where()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12}.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Name == "My Document" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void WhereScalar()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Scalar == 12 select doc;

            Assert.That(result.Single().Scalar, Is.EqualTo(12));
        }

        [Test, Ignore("TODO: *:* -Scaler:[* TO *]")]
        public void WhereNullScalar()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Scalar == null select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

    }
 
}