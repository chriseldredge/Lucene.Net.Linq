using System;
using System.Collections.Generic;
using Lucene.Net.Linq.Mapping;
using Lucene.Net.Search;

namespace Lucene.Net.Linq.Tests.Translation.ExpressionVisitors
{
    internal class FieldMappingInfoProviderStub : IFieldMappingInfoProvider
    {
        public IFieldMappingInfo GetMappingInfo(string propertyName)
        {
            return new FakeFieldMappingInfo { FieldName = propertyName };
        }

        public IEnumerable<string> AllProperties
        {
            get { return KeyProperties; }
        }

        public IEnumerable<string> IndexedProperties
        {
            get { return KeyProperties; }
        }

        public IEnumerable<string> KeyProperties
        {
            get { return new[] {"Id"}; }
        }

        public Query CreateMultiFieldQuery(string pattern)
        {
            throw new NotSupportedException();
        }
    }
}
