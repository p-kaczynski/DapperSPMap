using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Dapper;

namespace DapperSPMap
{
    internal class SprocMappingExpression<TModel> : ISprocMappingExpression<TModel>
    {
        private readonly List<SprocMemberExpression<TModel>> _expressions = new List<SprocMemberExpression<TModel>>();
        private bool _isSingle;
        private bool _isMultiple;

        ISingleSprocMappingExpression<TModel> ISingleSprocMappingExpression<TModel>.Use(Func<TModel, object> member,
            Action<ISprocMappingConfigurationExpression> configuration)
        {
            _isSingle = true;
            var memberExpression = new SprocMemberExpression<TModel>(member, Operations.Use);
            configuration(memberExpression);
            _expressions.Add(memberExpression);
            return this;
        }


        IMultipleSprocMappingExpression<TModel> IMultipleSprocMappingExpression<TModel>.GroupBy(
            Func<TModel, object> member, Action<ISprocMappingConfigurationExpression> configuration)
        {
            _isMultiple = true;
            var memberExpression = new SprocMemberExpression<TModel>(member, Operations.GroupBy);
            configuration(memberExpression);
            _expressions.Add(memberExpression);
            return this;
        }

        IMultipleSprocMappingExpression<TModel> IMultipleSprocMappingExpression<TModel>.Aggregate(
            Func<TModel, object> member, Action<IAggregateSprocMappingConfigurationExpression> configuration)
        {
            _isMultiple = true;
            var memberExpression = new SprocMemberExpression<TModel>(member, Operations.Aggregate);
            configuration(memberExpression);
            _expressions.Add(memberExpression);
            return this;
        }


        public void AssertConfigurationIsValid()
        {
            if (_isSingle == _isMultiple)
                throw new SprocMapperConfigurationException(
                    "Mapping expression cannot be single and multiple at the same time!");
        }

        private enum Operations
        {
            GroupBy,
            Aggregate,
            Use
        }

        private class SprocMemberExpression<T> : ISprocMappingConfigurationExpression,
            IAggregateSprocMappingConfigurationExpression
        {
            public Func<T, object> MemberSelector { get; private set; }
            public Operations Operation { get; private set; }
            public string ParameterName { get; private set; }

            private static int _id;
            //Aggregate
            public Func<IEnumerable<object>, string> AggregationStrategy { get; private set; }

            public SprocMemberExpression(Func<T, object> memberSelector, Operations operation)
            {
                MemberSelector = memberSelector;
                Operation = operation;
                // TODO (PK): Maybe some better naming? or a way of forcing users to choose one
                ParameterName = "param" + Interlocked.Increment(ref _id);

                AggregationStrategy = objects => string.Join(",", objects);
            }

            ISprocMappingConfigurationExpression ISprocMappingConfigurationExpression.AsParameter(string name)
            {
                ParameterName = name;

                return this;
            }

            IAggregateSprocMappingConfigurationExpression IAggregateSprocMappingConfigurationExpression.AsParameter(string name)
            {
                ParameterName = name;

                return this;
            }

            IAggregateSprocMappingConfigurationExpression IAggregateSprocMappingConfigurationExpression.
                SetAggregationStrategy(Func<IEnumerable<object>, string> strategy)
            {
                AggregationStrategy = strategy;

                return this;
            }
        }

        public DynamicParameters GetParameters(TModel item)
        {
            if (!_isSingle)
                throw new SprocMapperConfigurationException(
                    "Mapping expresion is not configured as single entity expression - use GetParameterSets method");

            var uses = _expressions.Where(expr => expr.Operation == Operations.Use);

            var p = new DynamicParameters();

            foreach (var use in uses)
            {
                p.Add(use.ParameterName, use.MemberSelector(item));
            }

            return p;
        }

        public IEnumerable<DynamicParameters> GetParameterSets(IEnumerable<TModel> items)
        {
            if (!_isMultiple)
                throw new SprocMapperConfigurationException(
                    "Mapping expresion is not configured as multi-entity expression  - use GetParameters method");

            var groupings = _expressions.Where(expr => expr.Operation == Operations.GroupBy);
            var folds = _expressions.Where(expr => expr.Operation == Operations.Aggregate);

            return items.GroupBy(
                i =>
                    new DynamicGroupingKey<TModel>(groupings.ToDictionary(g => g.ParameterName, g => g.MemberSelector),
                        i))
                .Select(g =>
                {
                    var p = new DynamicParameters();

                    foreach (var key in g.Key.Values.Keys)
                        p.Add(key, g.Key.Values[key]);

                    foreach (var fold in folds)
                    {
                        var selector = fold.MemberSelector;
                        p.Add(fold.ParameterName, fold.AggregationStrategy(g.Select(entity => selector(entity))));
                    }

                    return p;
                });
        }
    }
}