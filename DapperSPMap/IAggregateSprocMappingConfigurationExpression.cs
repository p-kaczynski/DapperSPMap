using System;
using System.Collections.Generic;

namespace DapperSPMap
{
    public interface IAggregateSprocMappingConfigurationExpression : ISprocMappingConfigurationExpression
    {
        new IAggregateSprocMappingConfigurationExpression AsParameter(string name);
        IAggregateSprocMappingConfigurationExpression SetAggregationStrategy(Func<IEnumerable<object>, string> strategy);
    }
}