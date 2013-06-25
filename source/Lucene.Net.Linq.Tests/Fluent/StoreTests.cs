using System;
using Lucene.Net.Linq.Fluent;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Fluent
{
    [TestFixture]
    public class StoreTests : FluentDocumentMapperTestBase
    {
        [Test]
        public void Stored()
        {
            Test(p => p.Stored(), StoreMode.Yes);
        }

        [Test]
        public void NotStored()
        {
            Test(p => p.NotStored(), StoreMode.No);
        }

        protected void Test(Action<PropertyMap<Sample>> setStoreMode, StoreMode expectedStoreMode)
        {
            setStoreMode(map.Property(x => x.Name));
            var mapper = GetMappingInfo("Name");

            Assert.That(mapper.Store, Is.EqualTo(expectedStoreMode));
        }
    }
}