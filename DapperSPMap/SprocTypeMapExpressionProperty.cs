using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace DapperSPMap
{
    internal class SprocTypeMapExpressionProperty : ISprocTypeMapExpressionProperty
    {
        internal SprocTypeMapExpressionProperty(PropertyInfo property)
        {
            Property = property;
            Names = new HashSet<string>();
        }

        public PropertyInfo Property { get; private set; }
        public HashSet<string> Names { get; private set; }
        public ISprocTypeMapExpressionProperty MapAs(string columnName)
        {
            Names.Add(columnName);
            return this;
        }
    }
}