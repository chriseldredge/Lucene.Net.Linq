using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Lucene.Net.Linq.Tests
{
    public class JsonTypeConverter<T> : System.ComponentModel.TypeConverter
    {
        private Type _type = typeof (T);

        public static JsonSerializerSettings DefaultSettings { get; set; }

        static JsonTypeConverter ()
        {
            DefaultSettings = new JsonSerializerSettings
            {
                //DateTimeZoneHandling = DateTimeZoneHandling.Utc,
                Formatting = Formatting.None,
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Ignore,
                ObjectCreationHandling = ObjectCreationHandling.Replace
            };
        }        

        public override bool CanConvertFrom (System.ComponentModel.ITypeDescriptorContext context, Type sourceType)
        {
            return true;
        }

        public override bool CanConvertTo (System.ComponentModel.ITypeDescriptorContext context, System.Type destinationType)
        {
            return true;
        }

        // Overrides the ConvertFrom method of TypeConverter.
        public override object ConvertFrom (System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                return Newtonsoft.Json.JsonConvert.DeserializeObject ((string)value, _type, DefaultSettings);
            }
            return Newtonsoft.Json.JsonConvert.SerializeObject (value);
        }

        // Overrides the ConvertTo method of TypeConverter.
        public override object ConvertTo (System.ComponentModel.ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destinationType)
        {
            if (value is string)
            {
                if (destinationType == typeof (string))
                    return value;
                return Newtonsoft.Json.JsonConvert.DeserializeObject ((string)value, destinationType, DefaultSettings);
            }
            if (destinationType == typeof (string))
                return Newtonsoft.Json.JsonConvert.SerializeObject (value, DefaultSettings);
            return Newtonsoft.Json.JsonConvert.DeserializeObject (Newtonsoft.Json.JsonConvert.SerializeObject (value), destinationType, DefaultSettings);
        }
    }
}
