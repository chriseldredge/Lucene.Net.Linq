using System;
using System.Linq;
using Lucene.Net.Documents;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests
{
    [TestFixture]
    public class DocumentHolderTests
    {
        public class MappedDocument : DocumentHolder
        {
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
        }

        [Test]
        public void NewInstanceHasDocument()
        {
            Assert.That(new MappedDocument().Document, Is.Not.Null);
        }

        [Test]
        public void ChangeField()
        {
            var doc = new MappedDocument { Name = "Sample" };

            doc.Name = "New Name";

            Assert.That(doc.Name, Is.EqualTo("New Name"));
        }

        [Test]
        public void SetFieldToNullRemoves()
        {
            var doc = new MappedDocument { Name = "Sample" };

            doc.Name = null;

            Assert.That(doc.Document.GetFields().Select(f => f.Name()), Is.Empty);
        }

        [Test]
        public void SetReplacesField()
        {
            var doc = new MappedDocument {Name = "Sample"};

            doc.Name = "New Name";

            Assert.That(doc.Document.GetFields().Select(f => f.Name()), Is.EquivalentTo(new[] {"Name"}));
        }

        [Test]
        public void SetNumericReplacesField()
        {
            var doc = new MappedDocument { NullableInt = 10 };

            doc.NullableInt = 12;

            Assert.That(doc.Document.GetFields().Select(f => f.Name()), Is.EquivalentTo(new[] { "NullableInt" }));
        }

        [Test]
        public void Boolean()
        {
            var doc = new MappedDocument();

            Assert.That(doc.Boolean, Is.Null);

            doc.Boolean = false;

            Assert.That(doc.Boolean, Is.False);
        }

        [Test]
        public void Float()
        {
            var doc = new MappedDocument {Float = 1.9f};

            Assert.That(doc.Float, Is.EqualTo(1.9f));
        }

        [Test]
        public void Long()
        {
            var doc = new MappedDocument();

            doc.SetNumeric<long>("l", 1234L);

            Assert.That(doc.Document.GetFieldable("l").StringValue(), Is.EqualTo("1234"));
        }

        [Test]
        public void ULongUnsupported()
        {
            var doc = new MappedDocument();

            TestDelegate call = () => doc.SetNumeric<ulong>("l", 1234L);

            Assert.That(call, Throws.ArgumentException);
        }

        [Test]
        public void DecimalUnsupported()
        {
            var doc = new MappedDocument();
            
            TestDelegate call = () => doc.SetNumeric<decimal>("l", 1234L);

            Assert.That(call, Throws.ArgumentException);
        }

        [Test]
        public void UnsetFloatDefaultValue()
        {
            var doc = new MappedDocument();

            Assert.That(doc.Float, Is.EqualTo(default(float)));
        }

        [Test]
        public void DateTimeOffset()
        {
            var doc = new MappedDocument();

            var now = new DateTimeOffset(DateTime.Now);

            doc.ItemDate = now;

            Assert.That(doc.ItemDate, Is.EqualTo(now));
        }
    }
}