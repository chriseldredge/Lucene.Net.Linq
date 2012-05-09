using System;
using System.ComponentModel;
using System.Globalization;

namespace Lucene.Net.Linq.Converters
{
    public class DateTimeConverter : TypeConverter
    {
        private readonly string format;

        public DateTimeConverter(string format)
        {
            this.format = format;
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            return sourceType == typeof (string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return destinationType == typeof (string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType)
        {
            return ((DateTime) value).ToUniversalTime().ToString(format);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            return DateTime.SpecifyKind(DateTime.ParseExact((string) value, format, null), DateTimeKind.Utc);
        }
    }
}