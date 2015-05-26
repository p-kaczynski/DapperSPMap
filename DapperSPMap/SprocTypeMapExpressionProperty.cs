using System.Reflection;

namespace DapperSPMap
{
    internal class SprocTypeMapExpressionProperty : ISprocTypeMapExpressionProperty
    {
        internal SprocTypeMapExpressionProperty(PropertyInfo property)
        {
            Property = property;
        }

        public PropertyInfo Property { get; private set; }
        public string Name { get; private set; }
        public void MapAs(string columnName)
        {
            Name = columnName;
        }
    }
}