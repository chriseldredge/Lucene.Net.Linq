using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Documents;

namespace Lucene.Net.Linq
{
    public class DocumentHolder : IDocumentHolder
    {
        protected Document document;

        [ThreadStatic] private bool recursionGuard;

        public Document Document
        {
            get
            {
                // Prevent StackOverflowException when OnGetDocument accesses Document.
                if (recursionGuard) return document;

                recursionGuard = true;
                try
                {
                    OnGetDocument();
                    return document;
                }
                finally
                {
                    recursionGuard = false;
                }
            }
            set
            {
                document = value;
                OnSetDocument();
            }
        }

        public DocumentHolder()
        {
            document = new Document();
        }

        /// <summary>
        /// Subclasses may override this method to provide a hook
        /// that will execute just before the Document property is
        /// accessed.
        /// </summary>
        protected virtual void OnGetDocument()
        {
        }

        /// <summary>
        /// Subclasses may override this method to provide a hook
        /// that will execute just after the Document property is
        /// set.
        /// </summary>
        protected virtual void OnSetDocument()
        {
        }

        protected string Get(string fieldName)
        {
            return document.Get(fieldName);
        }

        protected IEnumerable<string> GetValues(string fieldName)
        {
            return document.GetValues(fieldName);
        }

        protected void Set(string fieldName, string value)
        {
            Set(fieldName, value, Field.Store.YES, Field.Index.ANALYZED);
        }

        protected void Set(string fieldName, string value, Field.Store store, Field.Index index)
        {
            document.RemoveFields(fieldName);

            if (value == null) return;
            
            
            document.Add(new Field(fieldName, value, store, index));
        }

        protected void Set(string fieldName, IEnumerable<string> values, Field.Store store, Field.Index index)
        {
            document.RemoveFields(fieldName);
            
            if (values == null) return;

            var fields = values.Select(v => new Field(fieldName, v, store, index));

            foreach (var f in fields) document.Add(f);
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
            var field = document.GetFieldable(fieldName);
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
            document.RemoveFields(fieldName);

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

            document.Add(field);
        }

    }
}