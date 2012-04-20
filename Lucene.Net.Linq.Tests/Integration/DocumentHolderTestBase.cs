using System;
using Lucene.Net.Documents;

namespace Lucene.Net.Linq.Tests.Integration
{
    public class DocumentHolderTestBase : IntegrationTestBase
    {
        public class MappedDocument : DocumentHolder
        {
            public string Name
            {
                get { return Get("Name"); }
                set { Set("Name", value, Field.Store.YES, Field.Index.ANALYZED); }
            }

            public string Id
            {
                get { return Get("Id"); }
                set { Set("Id", value, Field.Store.YES, Field.Index.ANALYZED); }
            }

            public int Scalar
            {
                get { return GetNumeric<int>("Scalar").GetValueOrDefault(); }
                set { SetNumeric<int>("Scalar", value); }
            }

            public long Long
            {
                get { return GetNumeric<long>("Long").GetValueOrDefault(); }
                set { SetNumeric<long>("Long", value); }
            }

            public int? NullableScalar
            {
                get { return GetNumeric<int>("NullableScalar"); }
                set { SetNumeric("NullableScalar", value); }
            }

            public bool Flag
            {
                get { return GetNumeric<bool>("Flag").GetValueOrDefault(); }
                set { SetNumeric<bool>("Flag", value); }
            }

            public Version Version
            {
                get { return new Version(Get("Version")); }
                set { Set("Version", value.ToString()); }
            }
        }

        protected void AddDocument(MappedDocument doc)
        {
            AddDocument(doc.Document);
        }
    }
}