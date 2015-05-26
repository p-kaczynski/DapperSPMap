using System;
using System.Linq.Expressions;
using Dapper;

namespace DapperSPMap
{
    public interface ISprocTypeMapExpression<TModel>
    {
        SqlMapper.ITypeMap IgnoreRest();
        SqlMapper.ITypeMap RestAsUsual();
        ISprocTypeMapExpression<TModel> Property<TProp>(Expression<Func<TModel, TProp>> propertySelector, Action<ISprocTypeMapExpressionProperty> action);
    }
}