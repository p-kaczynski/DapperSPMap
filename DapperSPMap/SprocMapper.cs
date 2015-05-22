using System;
using System.Collections.Generic;
using Dapper;

namespace DapperSPMap
{
    public static class SprocMapper
    {
        private static readonly Dictionary<Type, Dictionary<string, object>> Configuration =
            new Dictionary<Type, Dictionary<string, object>>();

        /// <summary>
        /// Creates a new named map for {TModel} that describres how to obtain <see cref="DynamicParameters"/> from it
        /// </summary>
        /// <typeparam name="TModel">any type to be mapped</typeparam>
        /// <param name="mapName">type-unique identifier of this map</param>
        /// <returns></returns>
        public static ISprocMappingExpression<TModel> CreateMap<TModel>(string mapName)
        {
            var expr = new SprocMappingExpression<TModel>();

            if (!Configuration.ContainsKey(typeof (TModel)))
                Configuration.Add(typeof (TModel), new Dictionary<string, object>());

            Configuration[typeof (TModel)].Add(mapName, expr);

            return expr;
        }

        /// <summary>
        /// Obtain single <see cref="DynamicParameters"/> object from provided {TModel} entity. The mapping used must be of single type.
        /// </summary>
        /// <typeparam name="TModel">item's type</typeparam>
        /// <param name="item">item to map into <see cref="DynamicParameters"/></param>
        /// <param name="mapName">mapping name used with <see cref="SprocMapper.CreateMap{TModel}"/> method</param>
        /// <returns>a single DynamicParameters object with parameters reflecting mapped item</returns>
        public static DynamicParameters GetParameters<TModel>(TModel item, string mapName)
        {
            var expr = GetExpression<TModel>(mapName);

            return expr.GetParameters(item);
        }

        /// <summary>
        /// Obtain a collection of <see cref="DynamicParameters"/> objects from provided {TModel} IEnumerable. The mapping used must be of multi type.
        /// </summary>
        /// <typeparam name="TModel">items type</typeparam>
        /// <param name="items">items to map to <see cref="DynamicParameters"/></param>
        /// <param name="mapName">mapping name used with <see cref="SprocMapper.CreateMap{TModel}"/> method</param>
        /// <returns>Grouped and/or aggregated items data, mapped into <see cref="DynamicParameters"/> objects</returns>
        public static IEnumerable<DynamicParameters> GetParameterSets<TModel>(IEnumerable<TModel> items, string mapName)
        {
            var expr = GetExpression<TModel>(mapName);

            return expr.GetParameterSets(items);
        }

        /// <summary>
        /// Retrieves named mapping expression for {TModel}, or throws if anything is wrong
        /// </summary>
        /// <typeparam name="TModel">type for which map is retrieved</typeparam>
        /// <param name="mapName">mapping name used with <see cref="SprocMapper.CreateMap{TModel}"/> method</param>
        /// <returns></returns>
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

        /// <summary>
        /// Remove specified map for {TModel} type - if exists.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="mapName"></param>
        public static void RemoveMap<TModel>(string mapName)
        {
            if (!Configuration.ContainsKey(typeof (TModel))) return;

            if (!Configuration[typeof (TModel)].ContainsKey(mapName)) return;

            Configuration[typeof (TModel)].Remove(mapName);
        }

        /// <summary>
        /// Remove all models for specified {TModel}, if any
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        public static void RemoveAllMaps<TModel>()
        {
            if (!Configuration.ContainsKey(typeof(TModel))) return;

            Configuration.Remove(typeof (TModel));
        }
    }
}