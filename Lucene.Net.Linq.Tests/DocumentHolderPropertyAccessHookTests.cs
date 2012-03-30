using System;
using System.Linq;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class DocumentHolderPropertyAccessHookTests
    {
        public class MappedDocument : DocumentHolder
        {
            public int DocumentAccessCount { get; private set; }
            public int DocumentSetCount { get; private set; }

            public string Name
            {
                get { return Get("Name"); }
                set { Set("Name", value); }
            }

            public int? NullableInt
            {
                get { return GetNumeric<int>("NullableInt"); }
                set { SetNumeric("NullableInt", value); }
            }

            public float Float
            {
                get { return GetNumeric<float>("Float").GetValueOrDefault(); }
                set { SetNumeric<float>("Float", value); }
            }

            public DateTimeOffset? ItemDate
            {
                get { return GetDateTimeOffset("ItemDate"); }
                set { SetDateTimeOffset("ItemDate", value); }
            }

            public bool? Boolean
            {
                get { return GetNumeric<bool>("Boolean"); }
                set { SetNumeric("Boolean", value); }
            }

            public new T? GetNumeric<T>(string fieldName) where T : struct
            {
                return base.GetNumeric<T>(fieldName);
            }

            public new void SetNumeric<T>(string fieldName, T? value) where T : struct
            {
                base.SetNumeric(fieldName, value);
            }

            protected override void OnGetDocument()
            {
                DocumentAccessCount++;
            }

            protected override void OnSetDocument()
            {
                DocumentSetCount++;
            }
        }

        public class FieldAddingDocument : DocumentHolder
        {
            protected override void OnGetDocument()
            {
                Document.Add(new Field("another field", "another value", Field.Store.YES, Field.Index.NO));
            }
        }

        [Test]
        public void CtrDoesNotCallHook()
        {
            var doc = new MappedDocument();

            Assert.That(doc.DocumentSetCount, Is.EqualTo(0));
        }

        [Test]
        public void SetDocumentCallsHook()
        {
            var doc = new MappedDocument();

            doc.Document = new Document();

            Assert.That(doc.DocumentSetCount, Is.EqualTo(1));
        }

        [Test]
        public void GetDocumentCallsHook()
        {
            var doc = new MappedDocument();

            var d1 = doc.Document;
            var d2 = doc.Document;

            Assert.That(doc.DocumentAccessCount, Is.EqualTo(2));
        }

        [Test]
        public void GetDocumentHookCanAccessDocument()
        {
            var doc = new FieldAddingDocument();

            var document = doc.Document;

            Assert.That(document.Get("another field"), Is.Not.Null);
        }

        [Test]
        public void SetFieldDoesNotCallHook()
        {
            var doc = new MappedDocument();

            doc.Name = "Sample";

            Assert.That(doc.DocumentAccessCount, Is.EqualTo(0));
        }

        [Test]
        public void GetFieldDoesNotCallHook()
        {
            var doc = new MappedDocument();

            var key = "" + doc.Boolean + doc.Float + doc.ItemDate + doc.Name;

            Assert.That(doc.DocumentAccessCount, Is.EqualTo(0));
        }
    }
}