using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;
using Lucene.Net.Documents;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Fluent
{
    /// <summary>
    /// A fluent interface for specifying how a class is mapped to Lucene
    /// <see cref="Document"/>s.
    /// </summary>
    /// <typeparam name="T">The type of class being mapped.</typeparam>
    public class ClassMap<T>
    {
        private readonly Version version;
        private readonly ISet<PropertyMap<T>> properties = new HashSet<PropertyMap<T>>(new PartComparer<T>());
        private readonly IDictionary<string, string> documentKeys = new Dictionary<string, string>();
        private ReflectionScoreMapper<T> scoreMapper;
        private ReflectionDocumentBoostMapper<T> docBoostMapper;

        public ClassMap(Version version)
        {
            this.version = version;
        }

        /// <summary>
        /// Maps the property contained in <paramref name="expression"/>
        /// to a Lucene field.
        /// </summary>
        /// <param name="expression">A simple MemberExpression or UnaryExpression
        /// containing a property accessor such as <c>x => x.MyPropertyName</c></param>
        /// <returns>A <see cref="PropertyMap{T}"/> that allows further customizatoin of
        /// how the field will be analyzed, stored and indexed.</returns>
        public PropertyMap<T> Property(Expression<Func<T, object>> expression)
        {
            var propInfo = GetMemberInfo<PropertyInfo>(expression.Body);

            var part = new PropertyMap<T>(this, propInfo);

            properties.Add(part);

            return part;
        }

        /// <summary>
        /// Defines a property, similarly to <see cref="Property"/>, that acts
        /// as part of a unique key that identifies a <see cref="Document"/>
        /// and ensures that only one instance of that document will be present
        /// in an Index. May be specified for multiple properties to create
        /// a composite key, and may also be specified in addition to using
        /// <see cref="DocumentKey"/>.
        /// </summary>
        public PropertyMap<T> Key(Expression<Func<T, object>> expression)
        {
            var propInfo = GetMemberInfo<PropertyInfo>(expression.Body);

            var part = new PropertyMap<T>(this, propInfo, isKey:true);

            properties.Add(part);

            return part;
        }

        /// <summary>
        /// Defines a property that is used to set the document boost.
        /// </summary>
        public void DocumentBoost(Expression<Func<T, float>> expression)
        {
            var propInfo = GetMemberInfo<PropertyInfo>(expression.Body);

            docBoostMapper = new ReflectionDocumentBoostMapper<T>(propInfo);
        }

        /// <summary>
        /// Defines a property to be set with the value of <see cref="ScoreDoc.Score"/>
        /// when retrieving query results. See <seealso cref="QueryScoreAttribute"/>.
        /// </summary>
        public void Score(Expression<Func<T, object>> expression)
        {
            var propInfo = GetMemberInfo<PropertyInfo>(expression.Body);

            scoreMapper = new ReflectionScoreMapper<T>(propInfo);
        }
        
        /// <summary>
        /// Similar to <see cref="DocumentKeyAttribute"/>; adds a
        /// fixed field/value to the mapping that helps ensure that
        /// unrelated documents are excluded from queries when
        /// executing queries, updating or deleting documents.
        /// </summary>
        public DocumentKeyPart<T> DocumentKey(string fieldName)
        {
            documentKeys.Add(fieldName, null);
            return new DocumentKeyPart<T>(this, fieldName);
        }

        /// <summary>
        /// Converts the fluent specification into an <see cref="IDocumentMapper{T}"/>
        /// suitable for use with <see cref="LuceneDataProvider"/>.
        /// </summary>
        public IDocumentMapper<T> ToDocumentMapper()
        {
            var docMapper = new FluentDocumentMapper<T>(version);
            foreach (var p in properties)
            {
                var fieldMapper = p.ToFieldMapper();
                
                if (p.IsKey)
                {
                    docMapper.AddKeyField(fieldMapper);
                }
                else
                {
                    docMapper.AddField(fieldMapper);
                }
            }

            foreach (var kv in documentKeys)
            {
                if (ReferenceEquals(kv.Value, null))
                {
                    throw new InvalidOperationException("Must specify non-null document key value for key field " + kv.Key);
                }
                docMapper.AddKeyField(new DocumentKeyFieldMapper<T>(kv.Key, kv.Value));
            }

            if (scoreMapper != null)
            {
                docMapper.AddField(scoreMapper);
            }

            if (docBoostMapper != null)
            {
                docMapper.AddField(docBoostMapper);
            }
            
            return docMapper;
        }

        private TMemberType GetMemberInfo<TMemberType>(Expression expression) where TMemberType : MemberInfo
        {
            MemberExpression memberExpression;

            if (expression.NodeType == ExpressionType.Convert)
            {
                var body = (UnaryExpression)expression;
                memberExpression = (MemberExpression)body.Operand;
            }
            else if (expression.NodeType == ExpressionType.MemberAccess)
            {
                memberExpression = (MemberExpression)expression;
            }
            else
            {
                throw new InvalidOperationException("Unsupported expression " + expression);
            }

            return (TMemberType)(memberExpression).Member;
        }

        internal void AddProperty(PropertyMap<T> part)
        {
            properties.Remove(part);
            properties.Add(part);
        }

        internal void SetDocumentKeyValue(string fieldName, string value)
        {
            documentKeys[fieldName] = value;
        }

        /// <summary>
        /// Ensures ISet contains unique <see cref="PropertyMap{T}"/>s by
        /// PropertyName.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        internal class PartComparer<T> : IEqualityComparer<PropertyMap<T>>
        {
            public bool Equals(PropertyMap<T> x, PropertyMap<T> y)
            {
                return x.PropertyName.Equals(y.PropertyName);
            }

            public int GetHashCode(PropertyMap<T> obj)
            {
                return obj.PropertyName.GetHashCode();
            }
        }
    }
}
