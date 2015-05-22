using System.Collections.Generic;
using System.Linq;
using Dapper;
using Should;
using Xunit;

namespace DapperSPMap.Tests
{
    public class SprocMapperTests
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
            
            var dynParamLookup = dynParams as SqlMapper.IParameterLookup;
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
    }

    public class MyModel
    {
        public int Id { get; set; }
        public string Description { get; set; }
        public int Value { get; set; }
    }
}
