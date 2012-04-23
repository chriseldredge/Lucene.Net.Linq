using System;
using System.ComponentModel;
using System.Globalization;

namespace Lucene.Net.Linq.Tests
{
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
}