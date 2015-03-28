using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Linq.Search;
using Lucene.Net.Search;
using NUnit.Framework;
using LuceneVersion = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Tests.Mapping
{
    [TestFixture]
    public class ReflectionDocumentMapperTests
    {
        private ReflectedDocument item1;
        private ReflectedDocument item2;

        [SetUp]
        public void SetUp()
        {
            item1 = new ReflectedDocument { Id = "1", Version = new Version("1.2.3.4"), Location = "New York", Name = "Fun things", Number = 12 };
            item2 = new ReflectedDocument { Id = "1", Version = new Version("1.2.3.4"), Location = "New York", Name = "Fun things", Number = 12 };
        }

        [Test]
        public void CtrFindsKeyFields()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);
            Assert.That(mapper.KeyProperties, Is.EquivalentTo(new[] {"Id", "Version", "Number"}));
        }

        [Test]
        public void CtrFindsDocumentKeys()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocumentWithKey>(LuceneVersion.LUCENE_30);
            Assert.That(mapper.KeyProperties, Is.EquivalentTo(new[] { "Id", "Version", "Number", "**DocumentKey**Type", "**DocumentKey**Revision" }));
        }

        [Test]
        public void ContainsMetaPropertyForDocumentKey()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocumentWithKey>(LuceneVersion.LUCENE_30);

            Assert.That(mapper.KeyProperties.Select(mapper.GetMappingInfo).Count(), Is.EqualTo(5));
        }

        [Test]
        public void ToKey_ThrowsOnNullValues()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);

            TestDelegate call = () => mapper.ToKey(new ReflectedDocument());

            Assert.That(call, Throws.InvalidOperationException);
        }

        [Test]
        public void ToKey_DifferentInstance()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);
            var key1 = mapper.ToKey(new ReflectedDocument { Id = "x", Version = new Version("1.0") });
            var key2 = mapper.ToKey(new ReflectedDocument { Id = "x", Version = new Version("1.0") });
            Assert.That(key1, Is.Not.SameAs(key2));
        }

        [Test]
        public void ToKey_Equal()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);
            var key1 = mapper.ToKey(new ReflectedDocument { Id = "x", Version = new Version("1.0") });
            var key2 = mapper.ToKey(new ReflectedDocument { Id = "x", Version = new Version("1.0") });
            Assert.That(key1, Is.EqualTo(key2));
        }

        [Test]
        public void ToKey_NotEqual()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);
            var key1 = mapper.ToKey(new ReflectedDocument { Id = "x", Version = new Version("1.0") });
            var key2 = mapper.ToKey(new ReflectedDocument { Id = "y", Version = new Version("2.0") });
            Assert.That(key1, Is.Not.EqualTo(key2));
        }

        [Test]
        public void ToKey_DocumentKeys()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocumentWithKey>(LuceneVersion.LUCENE_30);

            mapper.ToKey(new ReflectedDocumentWithKey { Id = "x", Version = new Version("1.0") });
        }

        [Test]
        public void Documents_Equal()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);

            Assert.That(mapper.Equals(item1, item2), Is.True);
        }

        [Test]
        public void Documents_Equal_IgnoredField()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);

            item1.IgnoreMe = "different";

            Assert.That(mapper.Equals(item1, item2), Is.True);
        }

        [Test]
        public void Documents_Equal_Not()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);

            item1.Version = new Version("5.6.7.8");

            Assert.That(mapper.Equals(item1, item2), Is.False);
        }

        [Test]
        public void IsModified_Equal()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);
            var doc = new Document();
            mapper.ToDocument(item1, doc);

            Assert.That(mapper.IsModified(item1, doc), Is.False);
        }

        [Test]
        public void IsModified_IgnoredField()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);
            var doc = new Document();
            mapper.ToDocument(item1, doc);

            item1.IgnoreMe = "different";

            Assert.That(mapper.IsModified(item1, doc), Is.False);
        }

        [Test]
        public void IsModified_Equal_Not()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);
            var doc = new Document();
            mapper.ToDocument(item1, doc);

            item1.Version = new Version("5.6.7.8");

            Assert.That(mapper.IsModified(item1, doc), Is.True);
        }

        [Test]
        public void ValuesEqual_Enumerable()
        {
            var mapper = new ReflectionDocumentMapper<ReflectedDocument>(LuceneVersion.LUCENE_30);

            var result = mapper.ValuesEqual(new[] {"a", "b"}, new List<string> {"a", "b"});

            Assert.That(result, Is.True, "Should be equal when sequences are equal");
        }

        [Test]
        public void ReadOnlyKey()
        {
            var document = new Document();

            var mapper = new ReflectionDocumentMapper<ReflectedDocumentWithReadOnlyKey>(LuceneVersion.LUCENE_30);
            mapper.ToDocument(new ReflectedDocumentWithReadOnlyKey { Id = "a" }, document);

            Assert.That(document.GetField("Type").StringValue, Is.EqualTo("ReflectedDocumentWithReadOnlyKey"));
        }

        [Test]
        public void ScoreProperty()
        {
            var mapper = new ReflectionDocumentMapper<ScoreDoc>(LuceneVersion.LUCENE_30);

            Assert.That(mapper.AllProperties.ToArray(), Is.EqualTo(new [] {"Score"}));
        }

        public class ScoreDoc
        {
            [QueryScore]
            public float Score { get; set; }
        }

        public class ReflectedDocument
        {
            [Field(Key = true)]
            public string Id { get; set; }

            [Field(Converter = typeof(VersionConverter), Key = true)]
            public Version Version { get; set; }

            [Field]
            public string Name { get; set; }

            public string Location { get; set; }

            [NumericField(Key = true)]
            public int Number { get; set; }

            [IgnoreField]
            public string IgnoreMe { get; set; }

            [QueryScore]
            public decimal Score { get; set; }

			[ComputedField(typeof(FieldComputer))]
			public string ComputedField { get; set; }
        }

        [DocumentKey(FieldName = "Type", Value = "ReflectedDocumentWithKey")]
        [DocumentKey(FieldName = "Revision", Value = "1")]
        public class ReflectedDocumentWithKey : ReflectedDocument
        {
        }

        public class ReflectedDocumentWithReadOnlyKey
        {
            [Field(Key = true)]
            public string Id { get; set; }

            [Field(Key = true)]
            public string Type { get { return "ReflectedDocumentWithReadOnlyKey"; } }
        }

		public class FieldComputer : IComputedField
		{
			public object GetFieldValue(Document document)
			{
				return document.GetField("Name") + " " + document.GetField("Location");
			}

			public Query CreateQuery(string pattern)
			{
				throw new NotImplementedException();
			}

			public Query CreateRangeQuery(object lowerBound, object upperBound, RangeType lowerRange, RangeType upperRange)
			{
				throw new NotImplementedException();
			}

			public SortField CreateSortField(bool reverse)
			{
				throw new NotImplementedException();
			}

			public string ConvertToQueryExpression(object value)
			{
				throw new NotImplementedException();
			}
		}
    }

    
}