using System;
using Lucene.Net.Linq.Fluent;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Fluent
{
    [TestFixture]
    public class TermVectorModeTests : FluentDocumentMapperTestBase
    {
        [Test]
        public void No()
        {
            Test(p => p.WithTermVector.No(), TermVectorMode.No);
        }

        [Test]
        public void AnalyzedNoNorms()
        {
            Test(p => p.WithTermVector.Yes(), TermVectorMode.Yes);
        }

        [Test]
        public void NotAnalyzed()
        {
            Test(p => p.WithTermVector.Offsets(), TermVectorMode.WithOffsets);
        }

        [Test]
        public void NotAnalyzedNoNorms()
        {
            Test(p => p.WithTermVector.Positions(), TermVectorMode.WithPositions);
        }

        [Test]
        public void WithPositionsAndOffsets()
        {
            Test(p => p.WithTermVector.PositionsAndOffsets(), TermVectorMode.WithPositionsAndOffsets);
        }

        protected void Test(Action<PropertyMap<Sample>> setIndexMode, TermVectorMode expectedTermVectorMode)
        {
            setIndexMode(map.Property(x => x.Name));
            var mapper = GetMappingInfo("Name");

            Assert.That(mapper.TermVector, Is.EqualTo(expectedTermVectorMode));
        }
    }
}