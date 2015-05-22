using System;
using System.Collections.Generic;
using Dapper;

namespace DapperSPMap
{
    public static class SprocMapper
    {
        private static readonly Dictionary<Type, Dictionary<string, object>> Configuration =
            new Dictionary<Type, Dictionary<string, object>>();

        public static ISprocMappingExpression<TModel> CreateMap<TModel>(string mapName)
        {
            var expr = new SprocMappingExpression<TModel>();

            if (!Configuration.ContainsKey(typeof (TModel)))
                Configuration.Add(typeof (TModel), new Dictionary<string, object>());

            Configuration[typeof (TModel)].Add(mapName, expr);

            return expr;
        }

        public static DynamicParameters GetParameters<TModel>(TModel item, string mapName)
        {
            var expr = GetExpression<TModel>(mapName);

            return expr.GetParameters(item);
        }

        public static IEnumerable<DynamicParameters> GetParameterSets<TModel>(IEnumerable<TModel> items, string mapName)
        {
            var expr = GetExpression<TModel>(mapName);

            return expr.GetParameterSets(items);
        }

        private static ISprocMappingExpression<TModel> GetExpression<TModel>(string mapName)
        {
            if (!Configuration.ContainsKey(typeof (TModel)))
                throw new SprocMapperConfigurationException(string.Format("No maps for type {0} has been configured",
                    typeof (TModel).Name));

            if (!Configuration[typeof (TModel)].ContainsKey(mapName))
                throw new SprocMapperConfigurationException(
                    string.Format("For type {0} there is no mapping with a name of {1}", typeof (TModel).Name, mapName));

            var expr = Configuration[typeof (TModel)][mapName] as ISprocMappingExpression<TModel>;

            if (expr == null)
                throw new SprocMapperConfigurationException(
                    string.Format(
                        "Object under type {0}, map name {1} doesn't cast into ISprocMappingExpression<{0}> - this should not be possible!",
                        typeof (TModel).Name, mapName));

            expr.AssertConfigurationIsValid();
            return expr;
        }

        public static void RemoveMap<TModel>(string mapName)
        {
            if (!Configuration.ContainsKey(typeof (TModel))) return;

            if (!Configuration[typeof (TModel)].ContainsKey(mapName)) return;

            Configuration[typeof (TModel)].Remove(mapName);
        }

        public static void RemoveAllMaps<TModel>()
        {
            if (!Configuration.ContainsKey(typeof(TModel))) return;

            Configuration.Remove(typeof (TModel));
        }
    }
}