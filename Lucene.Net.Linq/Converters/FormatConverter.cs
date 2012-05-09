using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;

namespace Lucene.Net.Linq.Converters
{
    internal class FormatConverter : TypeConverter
    {
        private readonly string format;
        private readonly MethodInfo parseMethod;

        public FormatConverter(Type type, string format)
        {
            this.format = format;

            const BindingFlags bindingFlags = BindingFlags.Static | BindingFlags.Public | BindingFlags.InvokeMethod;

            var dateTimeSpecificArguments = new[] {typeof (string), typeof (string), typeof (IFormatProvider), typeof (DateTimeStyles)};
            var genericArguments = new[] {typeof (string), typeof (string), typeof (IFormatProvider)};

            parseMethod = type.GetMethod("ParseExact", bindingFlags, null, dateTimeSpecificArguments, null)
                ?? type.GetMethod("ParseExact", bindingFlags, null, genericArguments, null);

            if (parseMethod == null)
            {
                throw new ArgumentException("The type " + type + " does not declare a public static ParseExact(string, string, IFormatProvider) method.", "type");
            }
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
            return value == null ? null : ((IFormattable)value).ToString(format, null);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value)
        {
            var args = new List<object> {value, format, culture};

            if (parseMethod.GetParameters().Length == 4)
            {
                args.Add(DateTimeStyles.AssumeUniversal);
            }

            return parseMethod.Invoke(null, args.ToArray());
        }
    }
}