using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using Dapper;

namespace DapperSPMap
{
    internal class SprocTypeMapExpression<TModel> : ISprocTypeMapExpression<TModel>
    {
        private readonly List<SprocTypeMapExpressionProperty> _properties = new List<SprocTypeMapExpressionProperty>();

        public SqlMapper.ITypeMap IgnoreRest()
        {
            return GetTypeMap();
        }

        public SqlMapper.ITypeMap RestAsUsual()
        {
            return new FallbackTypeMapper(new[]
            {
                GetTypeMap(),
                new DefaultTypeMap(typeof(TModel)) 
            });
        }

        private SqlMapper.ITypeMap GetTypeMap()
        {
            var dict = _properties.ToDictionary(p => p.Name, p => p.Property);
            return new CustomPropertyTypeMap(typeof (TModel), (type, columnName) => dict.ContainsKey(columnName) ? dict[columnName] : null);
        }

        public ISprocTypeMapExpression<TModel> Property<TProp>(Expression<Func<TModel, TProp>> propertySelector, Action<ISprocTypeMapExpressionProperty> action)
        {
            var prop = new SprocTypeMapExpressionProperty(GetPropertyInfoFrom(propertySelector));
            _properties.Add(prop);
            action(prop);
            return this;
        }

        private static PropertyInfo GetPropertyInfoFrom<TProp>(Expression<Func<TModel, TProp>> propertySelector)
        {
            var body = propertySelector.Body as MemberExpression;
            if (body != null)
            {
                return (PropertyInfo)body.Member;
            }
            throw new ArgumentException("The provided expression doesn't point to a property!");
        }
    }
}
