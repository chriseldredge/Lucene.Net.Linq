using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Lucene.Net.Analysis;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Integration
{
    [TestFixture]
    public class OrderByTests : DocumentHolderTestBase
    {
        [SetUp]
        public void AddDocuments()
        {
            AddDocument(new MappedDocument { Name = "c", Scalar = 3, Version = new Version(100, 0, 0) });
            AddDocument(new MappedDocument { Name = "a", Scalar = 1, Version = new Version(20, 0, 0) });
            AddDocument(new MappedDocument { Name = "b", Scalar = 2, Version = new Version(3, 0, 0) });

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
        public void OrderBy_Comparable()
        {
            var documents = provider.AsQueryable<MappedDocument>();

            var result = from d in documents orderby d.Version select d.Version.Major;

            Assert.That(result.ToArray(), Is.EqualTo(new[] { 3, 20, 100 }));
        }
    }
}