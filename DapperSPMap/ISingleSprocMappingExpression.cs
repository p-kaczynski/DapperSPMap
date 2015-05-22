using System;

namespace DapperSPMap
{
    public interface ISingleSprocMappingExpression<TModel> : IBaseSprocMappingExpression<TModel>
    {
        ISingleSprocMappingExpression<TModel> Use(Func<TModel, object> member,
            Action<ISprocMappingConfigurationExpression> configuration);
    }
}