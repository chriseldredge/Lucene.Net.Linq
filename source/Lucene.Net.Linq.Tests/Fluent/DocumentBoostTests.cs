using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Lucene.Net.Linq.Mapping;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Fluent
{
    [TestFixture]
    public class DocumentBoostTests : FluentDocumentMapperTestBase
    {
        [Test]
        public void CaptureDocumentBoost()
        {
            map.DocumentBoost(x => x.Boost);

            var mapper = GetMappingInfo<ReflectionDocumentBoostMapper<Sample>>("Boost");

            Assert.That(mapper, Is.Not.Null);
        }
    }
}
