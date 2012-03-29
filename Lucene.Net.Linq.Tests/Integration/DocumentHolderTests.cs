using System;
using System.Linq;
using Lucene.Net.Documents;
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
                get { return Document.Get("Name"); }
                set { Document.Add(new Field("Name", value, Field.Store.YES, Field.Index.NOT_ANALYZED)); }
            }

            public int? Scalar
            {
                get
                {
                    var field = Document.GetField("Scalar");

                    return (field == null) ? null : (int?)Convert.ToInt32(field.StringValue());
                }
                set
                {
                    Document.RemoveField("Scalar");

                    if (!value.HasValue) return;

                    var field = new NumericField("Scalar", Field.Store.YES, true);
                    field.SetIntValue(value.Value);
                    
                    Document.Add(field);
                }
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

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
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