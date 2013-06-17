using System.Linq;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Util;
using NUnit.Framework;

namespace Lucene.Net.Linq.Tests.Fluent
{
    [TestFixture]
    public class NumericTests : FluentDocumentMapperTestBase
    {
        [Test]
        public void Numeric()
        {
            map.Property(x => x.Id).AsNumericField();

            var info = GetMappingInfo<NumericReflectionFieldMapper<Sample>>("Id");
            
            Assert.That(info, Is.InstanceOf<NumericReflectionFieldMapper<Sample>>());
        }

        [Test]
        public void NumericKey()
        {
            map.Key(x => x.Id).AsNumericField();

            Assert.That(map.ToDocumentMapper().KeyProperties.ToArray(), Is.EqualTo(new[] {"Id"}));
        }

        [Test]
        public void NumericFieldName()
        {
            map.Property(x => x.Id).ToField("_id_").AsNumericField();

            var info = GetMappingInfo<NumericReflectionFieldMapper<Sample>>("Id");

            Assert.That(info.FieldName, Is.EqualTo("_id_"));
        }
        
        [Test]
        public void NumericWithDefaultPrecisionStep()
        {
            map.Property(x => x.Id)
                  .AsNumericField();

            var info = GetMappingInfo<NumericReflectionFieldMapper<Sample>>("Id");

            Assert.That(info.PrecisionStep, Is.EqualTo(NumericUtils.PRECISION_STEP_DEFAULT));
        }

        [Test]
        public void NumericWithPrecisionStep()
        {
            map.Property(x => x.Id)
                  .AsNumericField()
                  .WithPrecisionStep(6);

            var info = GetMappingInfo<NumericReflectionFieldMapper<Sample>>("Id");

            Assert.That(info.PrecisionStep, Is.EqualTo(6));
        }
    }
}