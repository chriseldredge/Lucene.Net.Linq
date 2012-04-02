using System.Linq;
using Lucene.Net.Analysis;
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
                get { return Get("Name"); }
                set { Set("Name", value, Field.Store.YES, Field.Index.ANALYZED); }
            }

            public string Id
            {
                get { return Get("Id"); }
                set { Set("Id", value, Field.Store.YES, Field.Index.ANALYZED); }
            }

            public int? Scalar
            {
                get { return GetNumeric<int>("Scalar"); }
                set { SetNumeric("Scalar", value); }
            }
        }

        protected override Analyzer GetAnalyzer(Util.Version version)
        {
            var a = new PerFieldAnalyzerWrapper(base.GetAnalyzer(version));
            //a.AddAnalyzer(new NumberAnalyzer());
            return a;
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
        public void Where_ExactMatch()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id == "X.Z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ExactMatch_CaseInsensitive()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id == "x.z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ExactMatch2()
        {
            AddDocument(new MappedDocument { Name = "Documents Bill", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "Bills Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Name == "\"Bills Document\"" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Bills Document"));
        }

        [Test]
        public void Where_NotAnalyzed_StartsWith()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id.StartsWith("x.z") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_NotAnalyzed_CaseInsensitive()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id.StartsWith("x.z") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_StartsWith()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Name.StartsWith("my") select doc;

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