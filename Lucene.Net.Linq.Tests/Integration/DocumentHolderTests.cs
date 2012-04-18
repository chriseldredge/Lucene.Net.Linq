using System;
using System.Linq;
using Lucene.Net.Analysis;
using NUnit.Framework;
using Version = global::Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class DocumentHolderTests : DocumentHolderTestBase
    {
        protected override Analyzer GetAnalyzer(Version version)
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

            var result = from doc in documents where doc.Name == "My" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_FlagEqualTrue()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Flag = true }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

// Not redundant because it generates a different Espression tree
// ReSharper disable RedundantBoolCompare
            var result = from doc in documents where doc.Flag == true select doc;
// ReSharper restore RedundantBoolCompare

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_Multiple()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Scalar = 12 }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = documents.Where(d => d.Scalar == 12).Where(d => d.Name == "My");

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_QueryOnAnonymousProjection()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Scalar = 12 }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents select new { DocName = doc.Name }).Where(d => d.DocName == "My");

            Assert.That(result.Single().DocName, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_QueryOnFlatteningProjection()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Scalar = 12 }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents select doc.Name).Where(d => d == "My");

            Assert.That(result.Single(), Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_QueryOnLambdaProjection()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Scalar = 12 }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents select Convert(doc)).Where(d => d.Name == "My");

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        public class ConvertedDocument
        {
            public string Name { get; set; }
        }

        private ConvertedDocument Convert(MappedDocument doc)
        {
            return new ConvertedDocument {Name = doc.Name};
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
        public void Where_IgnoresToLower()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Id.ToLower() == "x.z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_IgnoresToLowerWithinNullSafetyCondition()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Id = "X.Y.1.2" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Id = "X.Z.1.3" }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where (doc.Id != null ? doc.Id.ToLower() : null) == "x.z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ExactMatch_Phrase()
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
        public void Where_NotEqual()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents where doc.Name != "\"My Document\"" select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

        [Test]
        public void Where_NotNull()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = null, Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents where doc.Name != null select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

        [Test]
        public void Where_Null()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = null, Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents where doc.Name == null select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().Scalar, Is.EqualTo(12));
        }

        [Test]
        public void Where_ScalarEqual()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Scalar == 12 select doc;

            Assert.That(result.Single().Scalar, Is.EqualTo(12));
        }

        [Test]
        public void Where_ScalarNotEqual()
        {
            AddDocument(new MappedDocument { Name = "Other Document", Scalar = 11}.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Scalar != 11 select doc;

            Assert.That(result.Single().Scalar, Is.EqualTo(12));
        }


        [Test]
        public void Where_ScalarNotNull()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = (from doc in documents where doc.Scalar != null select doc).ToList();

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ScalarNull()
        {
            AddDocument(new MappedDocument { Name = "Other Document" }.Document);
            AddDocument(new MappedDocument { Name = "My Document", Scalar = 12 }.Document);

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from doc in documents where doc.Scalar == null select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

    }
 
}