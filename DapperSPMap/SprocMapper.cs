using System.Collections.Generic;
using Dapper;

namespace DapperSPMap
{
    public static class SprocMapper
    {
        public static ISprocMapperProvider Provider { get; set; }
        private static readonly ISprocMapperProvider DefaultProvider;

        static SprocMapper()
        {
            // Initialize with default, allow for replacing (for example for mocking)
            DefaultProvider = new SprocMapperProvider();
            RevertToDefaultProvider();
        }

        /// <summary>
        /// Reverts to default <see cref="ISprocMapperProvider"/> implementation
        /// </summary>
        /// <returns>The provider that's being currently used</returns>
        public static ISprocMapperProvider RevertToDefaultProvider()
        {
            var previous = Provider;
            Provider = DefaultProvider;
            return previous;
        }

        /// <summary>
        /// Creates a new named map for {TModel} that describres how to obtain <see cref="DynamicParameters"/> from it
        /// </summary>
        /// <typeparam name="TModel">any type to be mapped</typeparam>
        /// <param name="mapName">type-unique identifier of this map</param>
        /// <returns></returns>
        public static ISprocMappingExpression<TModel> CreateMap<TModel>(string mapName)
        {
            return Provider.CreateMap<TModel>(mapName);
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
            return Provider.GetParameters(item, mapName);
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
            return Provider.GetParameterSets(items, mapName);
        }

        /// <summary>
        /// Remove specified map for {TModel} type - if exists.
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        /// <param name="mapName"></param>
        public static void RemoveMap<TModel>(string mapName)
        {
            Provider.RemoveMap<TModel>(mapName);
        }

        /// <summary>
        /// Remove all models for specified {TModel}, if any
        /// </summary>
        /// <typeparam name="TModel"></typeparam>
        public static void RemoveAllMaps<TModel>()
        {
            Provider.RemoveAllMaps<TModel>();
        }

        public static ISprocTypeMapExpression<TModel> CreateTypeMap<TModel>()
        {
            return Provider.CreateTypeMap<TModel>();
        } 
    }
}
