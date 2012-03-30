using System;
using Lucene.Net.Documents;

namespace Lucene.Net.Linq
{
    public class DocumentHolder : IDocumentHolder
    {
        public Document Document { get; set; }

        public DocumentHolder()
        {
            Document = new Document();
        }

        protected string Get(string fieldName)
        {
            return Document.Get(fieldName);
        }

        protected void Set(string fieldName, string value)
        {
            Document.RemoveFields(fieldName);

            if (value == null) return;

            Document.Add(new Field(fieldName, value, Field.Store.YES, Field.Index.NOT_ANALYZED));
        }

        protected DateTimeOffset? GetDateTimeOffset(string fieldName)
        {
            var ticks = GetNumeric<long>(fieldName);

            if (ticks.HasValue)
            {
                return new DateTimeOffset(ticks.Value, TimeSpan.Zero);
            }

            return null;
        }

        protected void SetDateTimeOffset(string fieldName, DateTimeOffset? dateTimeOffset)
        {
            SetNumeric(fieldName, dateTimeOffset.HasValue ? dateTimeOffset.Value.UtcTicks : (long?)null);
        }

        protected T? GetNumeric<T>(string fieldName) where T : struct
        {
            var field = Document.GetFieldable(fieldName);
            if (field == null) return null;

            var stringValue = field.StringValue();

            if (typeof(T) == typeof(bool))
            {
                var bitField = (int)Convert.ChangeType(stringValue, typeof(int));
                stringValue = bitField != 0 ? Boolean.TrueString : Boolean.FalseString;
            }

            return (T)Convert.ChangeType(stringValue, typeof(T));
        }

        protected void SetNumeric<T>(string fieldName, T? value) where T : struct 
        {
            Document.RemoveFields(fieldName);

            if (!value.HasValue) return;
            
            var field = new NumericField(fieldName, Field.Store.YES, true);

            var number = value.Value;

            if (number is int || number is bool)
            {
                field.SetIntValue((int)Convert.ChangeType(value, typeof(int)));    
            }
            else if (number is long)
            {
                field.SetLongValue((long)Convert.ChangeType(value, typeof(long)));
            }
            else if (number is double)
            {
                field.SetDoubleValue((double)Convert.ChangeType(value, typeof(double)));
            }
            else if (number is float)
            {
                field.SetDoubleValue((float)Convert.ChangeType(value, typeof(float)));
            }
            else
            {
                throw new ArgumentException("The generic type " + typeof(T) + " could not be converted to NumericField (only Int32, Long, Double and Float are supported).");
            }
                
            Document.Add(field);
        }

    }
}