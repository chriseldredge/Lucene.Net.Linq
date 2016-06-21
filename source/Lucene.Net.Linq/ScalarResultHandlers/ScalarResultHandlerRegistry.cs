using System;
using System.Collections.Generic;
using Lucene.Net.Linq.Util;

namespace Lucene.Net.Linq.ScalarResultHandlers
{
    internal class ScalarResultHandlerRegistry : RegistryBase<ScalarResultHandlerRegistry, Type, ScalarResultHandler>
    {
        private static readonly ScalarResultHandlerRegistry instance = CreateDefault();

        public static ScalarResultHandlerRegistry Instance
        {
            get { return instance; }
        }

        protected override void RegisterForTypes(IEnumerable<Type> itemTypes)
        {
            itemTypes.Apply(RegisterForType);
        }

        private void RegisterForType(Type type)
        {
            var handler = (ScalarResultHandler)Activator.CreateInstance(type);
            Register(handler.SupportedTypes, handler);
        }

        public override ScalarResultHandler GetItem(Type key)
        {
            return GetItemExact(key);
        }
    }
}
