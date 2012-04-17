using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Lucene.Net.Linq.Util
{
    public static class ReflectionUtils
    {
        public static bool ReflectionEquals(object lhs, object rhs)
        {
            if (ReferenceEquals(lhs, rhs)) return true;
            if (lhs == null || rhs == null) return false;

            return AreTypeAndFieldsEqual(lhs, rhs, GetAllInstanceFields(lhs.GetType()));
        }

        public static bool ReflectionEquals(object lhs, object rhs, IEnumerable<FieldInfo> fields)
        {
            if (ReferenceEquals(lhs, rhs)) return true;
            if (lhs == null || rhs == null) return false;

            return AreTypeAndFieldsEqual(lhs, rhs, fields);
        }

        private static bool AreTypeAndFieldsEqual(object lhs, object rhs, IEnumerable<FieldInfo> fields)
        {
            if (lhs.GetType() != rhs.GetType()) return false;

            foreach (var field in fields)
            {
                var value1 = field.GetValue(rhs);
                var value2 = field.GetValue(lhs);

                if (value1 == null)
                {
                    if (value2 != null)
                        return false;
                }
                else if ((typeof(DateTime).IsAssignableFrom(field.FieldType)) ||
                         ((typeof(DateTime?).IsAssignableFrom(field.FieldType))))
                {
                    var dateString1 = ((DateTime)value1).ToLongDateString();
                    var dateString2 = ((DateTime)value2).ToLongDateString();
                    if (!dateString1.Equals(dateString2))
                    {
                        return false;
                    }
                    continue;
                }
                else if (typeof(IEnumerable).IsAssignableFrom(value1.GetType()))
                {
                    if (value2 == null)
                    {
                        return false;
                    }

                    if (!typeof(IEnumerable).IsAssignableFrom(value2.GetType()))
                    {
                        return false;
                    }

                    if (!((IEnumerable)value1).Cast<object>().SequenceEqual(((IEnumerable)value2).Cast<object>()))
                    {
                        return false;
                    }
                }
                else if (!value1.Equals(value2))
                    return false;
            }

            return true;
        }

        /// <summary>
        /// <see cref="ReflectionHashCode(object,System.Collections.Generic.IEnumerable{System.Reflection.FieldInfo})"/>
        /// Uses <see cref="GetAllInstanceFields"/> to obtain list of fields to include.
        /// </summary>
        public static int ReflectionHashCode(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }

            return ReflectionHashCode(obj, GetAllInstanceFields(obj.GetType()));
        }

        /// <summary>
        /// Computes a hash code for the given object using the specified fields.
        /// </summary>
        public static int ReflectionHashCode(object obj, IEnumerable<FieldInfo> fields)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (fields == null)
            {
                throw new ArgumentNullException("fields");
            }

            const int startValue = 17;
            const int multiplier = 59;

            var hashCode = startValue;

            foreach (var field in fields)
            {
                var value = field.GetValue(obj);

                if (value != null)
                    hashCode = hashCode * multiplier + value.GetHashCode();
            }

            return hashCode;
        }

        /// <summary>
        /// Returns list of all public and private fields on a given type
        /// including all base types except System.Object.
        /// </summary>
        public static List<FieldInfo> GetAllInstanceFields(Type t)
        {
            var fields = new List<FieldInfo>();

            while (t != null && t != typeof(object))
            {
                fields.AddRange(t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public));

                t = t.BaseType;
            }

            return fields;
        }
    }
}
