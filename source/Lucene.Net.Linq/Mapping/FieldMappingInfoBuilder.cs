﻿using System;
using System.Collections;
using System.ComponentModel;
using System.Reflection;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Standard;
using Lucene.Net.Linq.Analysis;
using Lucene.Net.Linq.Converters;
using Lucene.Net.Linq.Util;
using DateTimeConverter = Lucene.Net.Linq.Converters.DateTimeConverter;
using Version = Lucene.Net.Util.Version;

namespace Lucene.Net.Linq.Mapping
{
    internal class FieldMappingInfoBuilder
    {
        internal const string DefaultDateTimeFormat = "yyyy-MM-ddTHH:mm:ss";

        internal static IFieldMapper<T> Build<T>(PropertyInfo p)
        {
            return Build<T>(p, Version.LUCENE_30, null);
        }

        internal static IFieldMapper<T> Build<T>(PropertyInfo p, Version version, Analyzer externalAnalyzer)
        {
            var boost = p.GetCustomAttribute<DocumentBoostAttribute>(true);

            if (boost != null)
            {
                return new ReflectionDocumentBoostMapper<T>(p);
            }

            var score = p.GetCustomAttribute<QueryScoreAttribute>(true);

            if (score != null)
            {
                return new ReflectionScoreMapper<T>(p);
            }

            var metadata = p.GetCustomAttribute<FieldAttribute>(true);
            var numericFieldAttribute = p.GetCustomAttribute<NumericFieldAttribute>(true);
            Type type;

            var isCollection = IsCollection(p.PropertyType, out type);

            // if a converter is provided, disable collection treatment
            if (metadata != null && metadata.Converter != null)
            {
                type = p.PropertyType;
                isCollection = false;
            }

            ReflectionFieldMapper<T> mapper;

            if (numericFieldAttribute != null)
            {
                mapper = NumericFieldMappingInfoBuilder.BuildNumeric<T>(p, type, numericFieldAttribute);
            }
            else
            {
                mapper = BuildPrimitive<T>(p, type, metadata, version, externalAnalyzer);
            }

            return isCollection ? new CollectionReflectionFieldMapper<T>(mapper, type) : mapper;
        }

        internal static bool IsCollection(Type type, out Type collectionType)
        {
            collectionType = type;

            if (type.IsGenericType && typeof(IEnumerable).IsAssignableFrom(type.GetGenericTypeDefinition()))
            {
                collectionType = type.GetGenericArguments()[0];
                return true;
            }

            return false;
        }

        private static ReflectionFieldMapper<T> BuildPrimitive<T>(PropertyInfo p, Type type, FieldAttribute metadata, Version version, Analyzer externalAnalyzer)
        {
            var fieldName = (metadata != null ? metadata.Field : null) ?? p.Name;
            var converter = GetConverter(p, type, metadata);
            var store = metadata != null ? metadata.Store : StoreMode.Yes;
            var index = metadata != null ? metadata.IndexMode : IndexMode.Analyzed;
            var termVectorMode = metadata != null ? metadata.TermVector : TermVectorMode.No;
            var boost = metadata != null ? metadata.Boost : 1.0f;
            var defaultParserOperator = metadata != null ? metadata.DefaultParserOperator : QueryParsers.QueryParser.Operator.OR;
            var caseSensitive = GetCaseSensitivity(metadata, converter);
            var analyzer = externalAnalyzer ?? BuildAnalyzer(metadata, converter, version);
            var nativeSort = metadata != null && metadata.NativeSort;

            return new ReflectionFieldMapper<T>(p, store, index, termVectorMode, converter, fieldName, defaultParserOperator, caseSensitive, analyzer, boost, nativeSort);
        }

        internal static Analyzer BuildAnalyzer(FieldAttribute metadata, TypeConverter converter, Version version)
        {
            if (metadata != null && metadata.Analyzer != null)
            {
                return CreateAnalyzer(metadata.Analyzer, version);
            }

            if (GetCaseSensitivity(metadata, converter))
            {
                return new KeywordAnalyzer();
            }

            return new CaseInsensitiveKeywordAnalyzer();
        }

        internal static bool GetCaseSensitivity(FieldAttribute metadata, TypeConverter converter)
        {
            if (metadata == null) return converter != null;

            return metadata.CaseSensitive ||
                   metadata.IndexMode == IndexMode.NotAnalyzed ||
                   metadata.IndexMode == IndexMode.NotAnalyzedNoNorms;
        }

        internal static TypeConverter GetConverter(PropertyInfo p, Type type, FieldAttribute metadata)
        {
            if (metadata != null && metadata.ConverterInstance != null)
            {
                return metadata.ConverterInstance;
            }

            var formatSpecified = metadata != null && metadata.Format != null;
            var format = (metadata != null ? metadata.Format : null) ?? DefaultDateTimeFormat;
            var propType = p.PropertyType.GetUnderlyingType();

            if (propType == typeof(DateTime))
            {
                return new DateTimeConverter(format);
            }

            if (formatSpecified || propType == typeof(DateTimeOffset))
            {
                return new FormatConverter(propType, format);
            }

            if (p.PropertyType == typeof(string)) return null;

            var converter = TypeDescriptor.GetConverter(type);

            if (converter == null || !converter.CanConvertFrom(typeof(string)))
            {
                throw new NotSupportedException("Property " + p.Name + " of type " + p.PropertyType + " cannot be converted from " + typeof(string));
            }
            return converter;
        }

        internal static Analyzer CreateAnalyzer(Type analyzer, Version version)
        {
            if (!typeof(Analyzer).IsAssignableFrom(analyzer))
            {
                throw new InvalidOperationException("The type " + analyzer + " does not inherit from " + typeof(Analyzer));
            }

            var versionCtr = analyzer.GetConstructor(new[] { typeof(Version) });

            if (versionCtr != null)
            {
                return (Analyzer)versionCtr.Invoke(new object[] { version });
            }

            var defaultCtr = analyzer.GetConstructor(new Type[0]);

            if (defaultCtr != null)
            {
                return (Analyzer)defaultCtr.Invoke(null);
            }

            throw new InvalidOperationException("The analyzer type " + analyzer + " must have a public default constructor or public constructor that accepts " + typeof(Version));
        }
    }
}