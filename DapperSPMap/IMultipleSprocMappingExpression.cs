using System;

namespace DapperSPMap
{
    public interface IMultipleSprocMappingExpression<TModel> : IBaseSprocMappingExpression<TModel>
    {
        IMultipleSprocMappingExpression<TModel> GroupBy(Func<TModel, object> member,
            Action<ISprocMappingConfigurationExpression> configuration);

        IMultipleSprocMappingExpression<TModel> Aggregate(Func<TModel, object> member,
            Action<IAggregateSprocMappingConfigurationExpression> configuration);
    }
}