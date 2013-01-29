using System;
using System.Collections.Generic;
using Lucene.Net.Linq.Util;
using Remotion.Linq.Utilities;

namespace Lucene.Net.Linq.Translation.ResultOperatorHandlers
{
    internal class ResultOperatorRegistry : RegistryBase<ResultOperatorRegistry, Type, ResultOperatorHandler>
    {
        protected override void RegisterForTypes(IEnumerable<Type> itemTypes)
        {
            itemTypes.Apply(RegisterForType);
        }

        private void RegisterForType(Type type)
        {
            var handler = (ResultOperatorHandler)Activator.CreateInstance(type);
            Register(handler.SupportedTypes, handler);
        }

        public override ResultOperatorHandler GetItem(Type key)
        {
            return GetItemExact(key);
        }
    }
}