using System;
using System.Collections.Generic;
using System.Linq;
using Dapper;
using Should;
using Xunit;

namespace DapperSPMap.Tests
{
    public class SprocMapperTests : IDisposable
    {
        [Fact]
        public void GetParameters_ReturnsCorrectParameters()
        {
            const string useMapName = "abcd";

            const string idParamName = "id_param";
            const string valParamName = "val_param";

            const int sampleId = 1;
            const string sampleDesc = "xyz";
            const int sampleValue = 2;

            SprocMapper.CreateMap<MyModel>(useMapName)
                .Use(obj => obj.Id, opt => opt.AsParameter(idParamName))
                .Use(obj => obj.Value.ToString(), opt => opt.AsParameter(valParamName));

            var model = new MyModel
            {
                Id = sampleId,
                Description = sampleDesc,
                Value = sampleValue
            };

            var dynParams = SprocMapper.GetParameters(model,useMapName);

            dynParams.ShouldNotBeNull();
            dynParams.ParameterNames.Count().ShouldEqual(2);
            dynParams.ParameterNames.ShouldContain(idParamName);
            dynParams.ParameterNames.ShouldContain(valParamName);
            dynParams.ParameterNames.ShouldNotContain("Description");
            
            var dynParamLookup = (SqlMapper.IParameterLookup) dynParams;
            dynParamLookup[idParamName].ShouldNotBeNull();
            dynParamLookup[valParamName].ShouldNotBeNull();

            dynParamLookup[idParamName].ShouldBeType<int>();
            dynParamLookup[valParamName].ShouldBeType<string>();

            dynParamLookup[idParamName].ShouldEqual(sampleId);
            dynParamLookup[valParamName].ShouldEqual(sampleValue.ToString());
        }

        [Fact]
        public void GetParameterSets_ReturnsCorrectParameters()
        {
            const string multiMapName = "qwert";
            const string valParamName = "val_param";
            const string idParamName = "id_param";

            SprocMapper.CreateMap<MyModel>(multiMapName)
                .GroupBy(m => m.Value, opt => opt.AsParameter(valParamName))
                .Aggregate(m => m.Id, opt =>
                    opt.AsParameter(idParamName)
                        .SetAggregationStrategy(objects => string.Join(":", objects)));

            var models = new List<MyModel>();
            for (var i = 0; i < 10; ++i)
            {
                models.Add(new MyModel
                {
                    Id = i,
                    Value = i/3,
                    Description = "Model number " + i
                });
            }
            
            var paramSets = SprocMapper.GetParameterSets(models, multiMapName).ToList();

            paramSets.ShouldNotBeNull();
            paramSets.Count.ShouldEqual(4);
            foreach (var set in paramSets.Cast<SqlMapper.IParameterLookup>())
            {
                set[idParamName].ShouldNotBeNull();
                set[valParamName].ShouldNotBeNull();
                set["Description"].ShouldBeNull();
            }
        }

        [Fact]
        public void GetParameters_ThrowsExceptionIfMultipleMapping()
        {
            const string sampleMapName = "sample name";
            // This creates a configuration for use with enumerable collections of MyModel entities
            SprocMapper.CreateMap<MyModel>(sampleMapName)
                .GroupBy(m => m.Id, opt=>opt.AsParameter("Id_name"))
                .GroupBy(m => m.Value, opt=>opt.AsParameter("Val_name"));

            Assert.Throws<SprocMapperConfigurationException>(
                () => SprocMapper.GetParameters(new MyModel(), sampleMapName));

        }

        [Fact]
        public void GetParameterSets_ReturnsMultipleParametersIfSingleMapping()
        {
            const string sampleMapName = "sample name";
            // This creates a configuration for use with enumerable collections of MyModel entities
            SprocMapper.CreateMap<MyModel>(sampleMapName)
                .Use(m => m.Id, opt => opt.AsParameter("Id_name"))
                .Use(m => m.Value, opt => opt.AsParameter("Val_name"));

            var models = new[]
            {
                new MyModel {Id = 1, Description = "1", Value = 101},
                new MyModel {Id = 2, Description = "2", Value = 102},
                new MyModel {Id = 3, Description = "3", Value = 103}
            };

            var result = SprocMapper.GetParameterSets(models, sampleMapName).ToList();

            result.Count.ShouldEqual(models.Length);
        }

        [Fact]
        public void GetParameters_ThrowsExceptionIfInvalidConfiguration()
        {
            const string sampleMapName = "sample name";
            // This creates a configuration for use with enumerable collections of MyModel entities
            var expr = SprocMapper.CreateMap<MyModel>(sampleMapName)
                .GroupBy(m => m.Id, opt => opt.AsParameter("Id_name"));
            // And using casting it sets also a single configuration
            ((ISingleSprocMappingExpression<MyModel>) expr).Use(m => m.Value, opt => opt.AsParameter("Val_name"));

            // Should always throw - for getting single and multiple sets
            Assert.Throws<SprocMapperConfigurationException>(
                () => SprocMapper.GetParameters(new MyModel(), sampleMapName));

            Assert.Throws<SprocMapperConfigurationException>(
                () => SprocMapper.GetParameterSets(new[] {new MyModel()}, sampleMapName));
        }

        [Fact]
        public void GetParameters_ThrowsExceptionIfModelNotMapped()
        {
            Assert.Throws<SprocMapperConfigurationException>(
                () => SprocMapper.GetParameters(new object(), "irrelevant name"));
        }

        [Fact]
        public void GetParameters_ThrowsExceptionIfMapNameNotFound()
        {
            const string sampleMapName = "sample name";
            // This creates a configuration for use with enumerable collections of MyModel entities
            SprocMapper.CreateMap<MyModel>(sampleMapName)
                .GroupBy(m => m.Id, opt => opt.AsParameter("Id_name"))
                .GroupBy(m => m.Value, opt => opt.AsParameter("Val_name"));

            SprocMapper.GetParameterSets(new[] { new MyModel { Id = 1, Value = 2 } }, sampleMapName).ShouldNotBeNull();

            Assert.Throws<SprocMapperConfigurationException>(
                () => SprocMapper.GetParameterSets(new[] { new MyModel { Id = 1, Value = 2 } }, sampleMapName + sampleMapName));
        }

        [Fact]
        public void GetParameters_RemoveMapDoesRemoveMap()
        {
            const string sampleMapName = "sample name";
            // This creates a configuration for use with enumerable collections of MyModel entities
            SprocMapper.CreateMap<MyModel>(sampleMapName)
                .GroupBy(m => m.Id, opt => opt.AsParameter("Id_name"))
                .GroupBy(m => m.Value, opt => opt.AsParameter("Val_name"));

            SprocMapper.GetParameterSets(new[] { new MyModel { Id = 1, Value = 2 } }, sampleMapName).ShouldNotBeNull();

            SprocMapper.RemoveMap<MyModel>(sampleMapName);

            Assert.Throws<SprocMapperConfigurationException>(
                () => SprocMapper.GetParameterSets(new[] { new MyModel { Id = 1, Value = 2 } }, sampleMapName));
        }

        [Fact]
        public void GetParameters_DefaultAggregationStrategyWorks()
        {
            const string sampleMapName = "sample name";
            const string idParamName = "Id_name";

            // This creates a configuration for use with enumerable collections of MyModel entities
            SprocMapper.CreateMap<MyModel>(sampleMapName)
                .Aggregate(m => m.Id, opt => opt.AsParameter(idParamName))
                .GroupBy(m => m.Value, opt => opt.AsParameter("val_name"));


            var models = new[]
            {
                new MyModel {Id = 1, Value = 1},
                new MyModel {Id = 2, Value = 1},
                new MyModel {Id = 3, Value = 1},
                new MyModel {Id = 4, Value = 1},
                new MyModel {Id = 5, Value = 1}
            };

            var parameterSets = SprocMapper.GetParameterSets(models, sampleMapName).ToList();
            parameterSets.Count.ShouldEqual(1); // all Value == 1
            var set = parameterSets.Single() as SqlMapper.IParameterLookup;
            set[idParamName].ShouldNotBeNull();
            set[idParamName].ShouldEqual("1,2,3,4,5");
        }

        [Fact]
        public void CreateTypeMap_ReturnsTypeMap()
        {
            SprocMapper.CreateTypeMap<MyModel>()
                .Property(model => model.Id, p => p.MapAs("id_param"))
                .Property(model => model.Value, p => p.MapAs("value_param"))
                .RestAsUsual()
                .ShouldNotBeNull();
        }

        [Fact]
        public void CreateTypeMap_RestAsUsualMapsCorrectly()
        {
            const string sampleIdParamName = "id_param";
            const string sampleValueParamName = "value_param";

            var map = SprocMapper.CreateTypeMap<MyModel>()
                .Property(model => model.Id, p => p.MapAs(sampleIdParamName))
                .Property(model => model.Value, p => p.MapAs(sampleValueParamName))
                .RestAsUsual();

            map.GetMember(sampleIdParamName).MemberType.ShouldEqual(typeof(int));
            map.GetMember(sampleIdParamName).Property.ShouldNotBeNull();
            map.GetMember(sampleIdParamName).Property.Name.ShouldEqual("Id");

            map.GetMember(sampleValueParamName).MemberType.ShouldEqual(typeof(int));
            map.GetMember(sampleValueParamName).Property.ShouldNotBeNull();
            map.GetMember(sampleValueParamName).Property.Name.ShouldEqual("Value");

            map.GetMember("Description").ShouldNotBeNull();
            map.GetMember("Description").MemberType.ShouldEqual(typeof(string));
            map.GetMember("Description").Property.ShouldNotBeNull();
            map.GetMember("Description").Property.Name.ShouldEqual("Description");
        }

        [Fact]
        public void CreateTypeMap_IgnoreRestMapsCorrectly()
        {
            const string sampleIdParamName = "id_param";
            const string sampleValueParamName = "value_param";

            var map = SprocMapper.CreateTypeMap<MyModel>()
                .Property(model => model.Id, p => p.MapAs(sampleIdParamName))
                .Property(model => model.Value, p => p.MapAs(sampleValueParamName))
                .IgnoreRest();

            map.GetMember(sampleIdParamName).MemberType.ShouldEqual(typeof(int));
            map.GetMember(sampleIdParamName).Property.ShouldNotBeNull();
            map.GetMember(sampleIdParamName).Property.Name.ShouldEqual("Id");

            map.GetMember(sampleValueParamName).MemberType.ShouldEqual(typeof(int));
            map.GetMember(sampleValueParamName).Property.ShouldNotBeNull();
            map.GetMember(sampleValueParamName).Property.Name.ShouldEqual("Value");

            map.GetMember("Description").ShouldBeNull();
        }

        [Fact]
        public void CreateTypeMap_ThrowsOnInvalidExpression()
        {
            Assert.Throws<ArgumentException>(() => SprocMapper.CreateTypeMap<MyModel>()
                .Property(model => model.Id.ToString(), p => p.MapAs("any name")));
        }

        public void Dispose()
        {
            SprocMapper.RemoveAllMaps<MyModel>();
            SprocMapper.RevertToDefaultProvider();
        }
    }

    public class MyModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
    }
}
