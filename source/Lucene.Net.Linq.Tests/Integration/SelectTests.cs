﻿using System;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Linq.Analysis;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class SelectTests : IntegrationTestBase
    {
        private PerFieldAnalyzerWrapper analyzer;

        protected override Analyzer GetAnalyzer(Net.Util.Version version)
        {
            analyzer = new PerFieldAnalyzerWrapper(base.GetAnalyzer(version));
            analyzer.AddAnalyzer<SampleDocument>(t => t.Id, new KeywordAnalyzer());
            analyzer.AddAnalyzer<SampleDocument>(t => t.Key, new CaseInsensitiveKeywordAnalyzer());
            return analyzer;
        }

        [Test]
        public void Select()
        {
            var d = new SampleDocument {Name = "My Document"};
            AddDocument(d);

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents select doc;

            Assert.That(result.FirstOrDefault().Name, Is.EqualTo(d.Name));
        }

        [DocumentKey(FieldName = "FixedKey", Value = "SampleDocument")]
        class AlternateDocument
        {
            [Field(Key = true)]
            public string Key { get; set; }

            [Field("Name")]
            public string AlternateName { get; set; }
        }

        [Test]
        public void StoresAndRetrievesByFieldName()
        {
            var d = new AlternateDocument { AlternateName = "My Document", Key = "0" };
            using (var session = provider.OpenSession<AlternateDocument>())
            {
                session.Add(d);
                session.Commit();
            }

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents select doc;

            Assert.That(result.FirstOrDefault().Name, Is.EqualTo(d.AlternateName));
        }

        [Test]
        public void SelectScalar()
        {
            const int scalar = 99;

            var d = new SampleDocument {Name = "a", NullableScalar = scalar};

            AddDocument(d);

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents select doc.NullableScalar;

            Assert.That(result.FirstOrDefault(), Is.EqualTo(scalar));
        }

        [Test]
        public void Where()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12});

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Name == "My" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void BinaryOrWithNegation()
        {
            AddDocument(new SampleDocument { Name = "Interstellar", Scalar = 2010});
            AddDocument(new SampleDocument { Name = "Gladiator", Scalar = 2000 });

            var documents = provider.AsQueryable<SampleDocument>();
            var result = documents.Where(d => d.Scalar == 2010 || d.Name != "Interstellar").ToList();

            Assert.That(result.Select(d => d.Name), Is.EquivalentTo(new[] {"Interstellar", "Gladiator"}));
        }

        [Test]
        public void Where_FlagEqualTrue()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", Flag = true });

            var documents = provider.AsQueryable<SampleDocument>();

// Not redundant because it generates a different Expression tree
// ReSharper disable RedundantBoolCompare
            var result = from doc in documents where doc.Flag == true select doc;
// ReSharper restore RedundantBoolCompare

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_Multiple()
        {
            AddDocument(new SampleDocument { Name = "Other Document", NullableScalar = 12 });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = documents.Where(d => d.NullableScalar == 12).Where(d => d.Name == "My");

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_QueryOnAnonymousProjection()
        {
            AddDocument(new SampleDocument { Name = "Other Document", NullableScalar = 12 });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = (from doc in documents select new { DocName = doc.Name }).Where(d => d.DocName == "My");

            Assert.That(result.Single().DocName, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_QueryOnFlatteningProjection()
        {
            AddDocument(new SampleDocument { Name = "Other Document", NullableScalar = 12 });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = (from doc in documents select doc.Name).Where(d => d == "My");

            Assert.That(result.Single(), Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_QueryOnLambdaProjection()
        {
            AddDocument(new SampleDocument { Name = "Other Document", NullableScalar = 12 });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = (from doc in documents select Convert(doc)).Where(d => d.Name == "My");

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ContainsAfterNullSafety()
        {
            AddDocument(new SampleDocument { Name = "a FOO doc" });
            AddDocument(new SampleDocument { Name = "doc", Id = "bar"});
            AddDocument(new SampleDocument { Name = "a BAZ doc", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = documents.Where(d => (d.Name != null ? d.Name.ToLower() : null).Contains("foo"));

            Assert.That(result.Count(), Is.EqualTo(1));
        }

        public class ConvertedDocument
        {
            public string Name { get; set; }
        }

        private ConvertedDocument Convert(SampleDocument doc)
        {
            return new ConvertedDocument {Name = doc.Name};
        }

        [Test]
        public void Where_ExactMatch()
        {
            AddDocument(new SampleDocument { Name = "Other Document", Id = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "My Document", Id = "X.Z.1.3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Id == "X.Z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_FieldNameDifferentFromProperty()
        {
            AddDocument(new SampleDocument { Name = "Other Document", Alias = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "My Document", Alias = "X.Z.1.3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Alias == "X.Z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_Numeric()
        {
            AddDocument(new SampleDocument { Name = "Other Document", Id = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "My Document", Id = "X.Z.1.3", Long = 423434L });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Long == 423434L select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ExactMatch_CaseInsensitive()
        {
            analyzer.AddAnalyzer<SampleDocument>(t => t.Id, new CaseInsensitiveKeywordAnalyzer());
            AddDocument(new SampleDocument { Name = "Other Document", Key = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "My Document", Key = "X.Z.1.3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Key == "x.z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_IgnoresToLower()
        {
            analyzer.AddAnalyzer<SampleDocument>(t => t.Id, new CaseInsensitiveKeywordAnalyzer());
            AddDocument(new SampleDocument { Name = "Other Document", Key = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "My Document", Key = "X.Z.1.3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Key.ToLower() == "x.z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_IgnoresToLowerWithinNullSafetyCondition()
        {
            analyzer.AddAnalyzer<SampleDocument>(t => t.Id, new CaseInsensitiveKeywordAnalyzer());
            AddDocument(new SampleDocument { Name = "Other Document", Key = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "My Document", Key = "X.Z.1.3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where (doc.Key != null ? doc.Key.ToLower() : null) == "x.z.1.3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_Keyword_StartsWith()
        {
            AddDocument(new SampleDocument { Name = "Other Document", Id = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "My Document", Id = "X.Z.1.3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Id.StartsWith("X.Z") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test(Description = "https://github.com/themotleyfool/Lucene.Net.Linq/issues/30")]
        public void Where_Keyword_WithSpace()
        {
            AddDocument(new SampleDocument { Name = "Other Document", Key = "1 2" });
            AddDocument(new SampleDocument { Name = "My Document", Key = "1 3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Key == "1 3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test(Description = "https://github.com/themotleyfool/Lucene.Net.Linq/issues/30")]
        public void Where_Keyword_WithSpaceAndAsterisk()
        {
            AddDocument(new SampleDocument { Name = "Other Document", Key = "1 * 2" });
            AddDocument(new SampleDocument { Name = "My Document", Key = "1 * 3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Key == "1 * 3" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_LowercaseKeyword_StartsWith()
        {
            analyzer.AddAnalyzer<SampleDocument>(t => t.Id, new CaseInsensitiveKeywordAnalyzer());
            AddDocument(new SampleDocument { Name = "Other Document", Key = "X.Y.1.2" });
            AddDocument(new SampleDocument { Name = "My Document", Key = "X.Z.1.3" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Key.StartsWith("x.z") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_StartsWith()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.Name.StartsWith("my") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_StartsWith_Negate()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where !doc.Name.StartsWith("other") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_StartsWith_Negate_NullSafe()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where (bool)(!((bool?)doc.Name.StartsWith("other"))) select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_Compare()
        {
            AddDocument(new SampleDocument { Name = "Other Document", Id = "b" });
            AddDocument(new SampleDocument { Name = "My Document", Id = "z" });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where string.Compare(doc.Id, "b") > 0 select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_NotEqual()
        {
            AddDocument(new SampleDocument { Name = "Other" });
            AddDocument(new SampleDocument { Name = "Mine", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = (from doc in documents where doc.Name != "Other" select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().Name, Is.EqualTo("Mine"));
        }

        [Test]
        public void Where_NotNull()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = null, NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = (from doc in documents where doc.Name != null select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

        [Test]
        public void Where_Null()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = null, NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = (from doc in documents where doc.Name == null select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().NullableScalar, Is.EqualTo(12));
        }

        [Test]
        public void Where_Blank()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = null, NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = (from doc in documents where doc.Name == "" select doc).ToList();

            Assert.That(result.Count(), Is.EqualTo(1));
            Assert.That(result.Single().NullableScalar, Is.EqualTo(12));
        }

        [Test]
        public void Where_ScalarEqual()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.NullableScalar == 12 select doc;

            Assert.That(result.Single().NullableScalar, Is.EqualTo(12));
        }

        [Test]
        public void Where_ScalarNotEqual()
        {
            AddDocument(new SampleDocument { Name = "Other Document", NullableScalar = 11});
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.NullableScalar != 11 select doc;

            Assert.That(result.Single().NullableScalar, Is.EqualTo(12));
        }


        [Test]
        public void Where_ScalarNotNull()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = (from doc in documents where doc.NullableScalar != null select doc).ToList();

            Assert.That(result.Single().Name, Is.EqualTo("My Document"));
        }

        [Test]
        public void Where_ScalarNull()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.NullableScalar == null select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

        [Test]
        public void Where_ThisAndNotThat()
        {
            AddDocument(new SampleDocument { Name = "Other", NullableScalar = 12 });
            AddDocument(new SampleDocument { Name = "My", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.NullableScalar == 12 && doc.Name != "Other" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("My"));
        }

        [Test]
        public void Where_AnyField()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.AnyField() == "other" select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

        [Test]
        public void Where_AnyField_StartsWith()
        {
            AddDocument(new SampleDocument { Name = "Other Document" });
            AddDocument(new SampleDocument { Name = "My Document", NullableScalar = 12 });

            var documents = provider.AsQueryable<SampleDocument>();

            var result = from doc in documents where doc.AnyField().StartsWith("ot") select doc;

            Assert.That(result.Single().Name, Is.EqualTo("Other Document"));
        }

        public class SampleWithCollection
        {
            [Field (Key = true)]
            public string Key { get; set; }

            [Field (Converter = typeof (HashSetConverter))]
            public System.Collections.Generic.HashSet<string> HashSetCollection { get; set; }
        }

        class HashSetConverter : JsonTypeConverter<System.Collections.Generic.HashSet<string>>
        {            
        }

        [Test]
        public void StoresAndRetrieves_CollectionWithConverter_HashSet ()
        {
            var d = new SampleWithCollection { Key = "0", HashSetCollection = new System.Collections.Generic.HashSet<string> { "item 1", "item 2", "item 3" } };
            using (var session = provider.OpenSession<SampleWithCollection> ())
            {
                session.Add (d);
                session.Commit ();
            }

            var documents = provider.AsQueryable<SampleWithCollection> ();

            var result = from doc in documents select doc;

            var field = result.FirstOrDefault ().HashSetCollection;

            Assert.IsTrue (field.Contains ("item 1"));            
        }

        [Test]
        public void StoresAndRetrieves_CollectionWithConverter_HashSet_Fluent ()
        {
            var converter = new JsonTypeConverter<System.Collections.Generic.HashSet<string>> ();
            var map = new Lucene.Net.Linq.Fluent.ClassMap<SampleWithCollection> (Lucene.Net.Util.Version.LUCENE_30);
            map.Key (x => x.Key);
            map.Property (x => x.HashSetCollection).NotIndexed ().ConvertWith (converter);

            var d = new SampleWithCollection { Key = "0", HashSetCollection = new System.Collections.Generic.HashSet<string> { "item 1", "item 2", "item 3" } };
            using (var session = provider.OpenSession<SampleWithCollection> (map.ToDocumentMapper ()))
            {
                session.Add (d);
                session.Commit ();
            }

            using (var session = provider.OpenSession<SampleWithCollection> (map.ToDocumentMapper ()))
            {
                var field = session.Query ().First ().HashSetCollection;

                Assert.IsTrue (field.Contains ("item 1"));
            }
        }

    }

}
