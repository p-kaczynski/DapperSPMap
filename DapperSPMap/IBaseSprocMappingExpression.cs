using System.Collections.Generic;
using Dapper;

namespace DapperSPMap
{
    public interface IBaseSprocMappingExpression
    {
        void AssertConfigurationIsValid();
    }

    public interface IBaseSprocMappingExpression<in TModel> : IBaseSprocMappingExpression
    {
        DynamicParameters GetParameters(TModel item);
        IEnumerable<DynamicParameters> GetParameterSets(IEnumerable<TModel> items);
    }
}