using System;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Documents;
using Lucene.Net.Index;
using Lucene.Net.Search;
using Lucene.Net.Store;
using Lucene.Net.Util;
using NUnit.Framework;
using Directory = Lucene.Net.Store.Directory;
using Version = System.Version;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class OrderByTests : DocumentHolderTestBase
    {
        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new MappedDocument { Name = "c", Scalar = 3, Flag = true, Version = new Version(100, 0, 0) });
            AddDocument(new MappedDocument { Name = "a", Scalar = 1, Version = new Version(20, 0, 0) });
            AddDocument(new MappedDocument { Name = "b", Scalar = 2, Flag = true, Version = new Version(3, 0, 0) });

            TypeDescriptor.AddAttributes(typeof(Version), new TypeConverterAttribute(typeof(VersionConverter)));
        }

        public class VersionConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
            {
                return sourceType == typeof(string);
            }

            public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
            {
                return new Version((string)value);
            }
        }

        protected override Analyzer GetAnalyzer(Net.Util.Version version)
        {
            var a = new PerFieldAnalyzerWrapper(base.GetAnalyzer(version));
            a.AddAnalyzer("Version", new KeywordAnalyzer());
            return a;
        }

        [Test]
        public void OrderBy_String()
        {
            var documents = provider.AsQueryable<MappedDocument>();

            var result = from d in documents orderby d.Name select d.Name;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { "a", "b", "c" }));
        }

        [Test]
        public void OrderBy_String_Desc()
        {
            var documents = provider.AsQueryable<MappedDocument>();

            var result = from d in documents orderby d.Name descending select d.Name;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { "c", "b", "a" }));
        }

        [Test]
        public void OrderBy_Int()
        {
            var documents = provider.AsQueryable<MappedDocument>();

            var result = from d in documents orderby d.Scalar select d.Scalar;

            Assert.That(result.ToArray(), Is.EqualTo(new int[] { 1, 2, 3 }));
        }

        [Test]
        public void OrderBy_Long()
        {
            writer.DeleteAll();
            AddDocument(new MappedDocument {Long = 23155163});
            AddDocument(new MappedDocument {Long = 4667});
            AddDocument(new MappedDocument {Long = 22468359});

            var documents = provider.AsQueryable<MappedDocument>();

            var result = from d in documents orderby d.Long select d.Long;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 4667L, 22468359L, 23155163L }));
        }

        [Test]
        public void OrderBy_Bool()
        {
            var documents = provider.AsQueryable<MappedDocument>();

            var result = from d in documents orderby d.Flag select d.Flag;

            Assert.That(result.ToArray(), Is.EqualTo(new [] { false, true, true }));
        }

        [Test]
        public void OrderBy_Comparable()
        {
            var documents = provider.AsQueryable<MappedDocument>();

            var result = from d in documents orderby d.Version select d.Version.Major;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 3, 20, 100 }));
        }
    }
}
