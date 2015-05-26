DapperSPMap
=============

# Dapper Stored Procedure Mapper

## Purpose
This solution provides a way to specify programatically the mapping between domain model and stored procedures input parameters for use with Dapper. This solves a problem for a very specific scenario, where the update/delete functionality is exposed via stored procedures, and their input allows for a batch operations.

Additionally the Mapper allows for simple fluent creation of type maps (SqlMapper.ITypeMap) for reading objects in.

## Usage
The only class one needs to use is `SprocMapper`. It exposes static methods to create maps and obtain Dapper's `DynamicParameters` from model classes.

### CreateMap
```
	public class MyModel{
		// This property can be aggregated into a list
		public int Id {get;set;}
		// This property is not relevant for update/delete
		public string Description {get;set;}
		// This property is required to be common for all aggregated values
		// public int Value {get;set;}
	}
	
	class MyModelRepository {
		// usage: [dbo].[UPDATE_MY_MODEL_ENTITIES] @value_param = 1, @id_list = '2,8,23,43,343'
		private const UpdateProcedureName = "[dbo].[UPDATE_MY_MODEL_ENTITIES]";
		
		static MyModelRepository(){
			SprocMapper.CreateMap<MyModel>(UpdateProcedureName)
                .GroupBy(m => m.Value, opt => opt.AsParameter("value_param"))
                .Aggregate(m => m.Id, opt => opt.AsParameter("id_list"));
		}
		
		public void UpdateMyModels(IEnumerable<MyModel> models){
			var parameterSets = SprocMapper.GetParameterSets(models, UpdateProcedureName);
			IDbConnection dbConnection = ...
			// Using Dapper
			dbConnection.Execute(UpdateProcedureName, parameterSets, null, null, CommandType.StoredProcedure);
		}
	}
```
In this example passing an enumerable of MyModel objects will result in calling the `[dbo].[UPDATE_MY_MODEL_ENTITIES]` as few times as possible - the entities will be grouped by `Value` property, and for each group a `DynamicParameters` object will be created with the Id properties flattened into a list.

### CreateTypeMap
```
	class MyModelRepository {
		// usage: [dbo].[UPDATE_MY_MODEL_ENTITIES] @value_param = 1, @id_list = '2,8,23,43,343'
		private const ViewProcedureName = "[dbo].[VIEW_MY_MODEL_ENTITIES]";
		
		static MyModelRepository(){
			SqlMapper.SetTypeMap(typeof(MyModel), 
				SprocMapper.CreateTypeMap<MyModel>()
					.Property(m => m.Value, opt => opt.MapAs("value_param"))
					.Property(m => m.Id, opt => opt.MapAs("id_list"))
					.RestAsUsual()
			);
		}
		
		public IEnumerable<MyModel> GetModels(){
			IDbConnection dbConnection = ...
			// Using Dapper
			return dbConnection.Query<MyModel>(ViewProcedureName, null, null, null, CommandType.StoredProcedure);
		}
	
```
Setting the type map in static constructor of the repository class keeps all the DB-specific information in one place, and (depending on your dependency management) should not fire unless that specific repository is called, saving few unnecessary operations.

## Other functionality
### Non-batch procedures
SprocMapper can be used for more common scenario where properties are not aggregated and one entity equals one stored procedure execution. It comes into play when the procedure parameters name differ from model properties names, or when some custom transforming is required. In this case a `Use` mapping can be created:

```
	SprocMapper.CreateMap<MyModelClass>(UpdateProcedureName)
		.Use(m=>m.Id, opt => opt.AsParameter("id_param_name"))
		.Use(m=>PerformTransformation(m.Value), opt => opt.AsParameter("value_param_name"));

	...
	
	DynamicParameters parametersForUpdateProcedure = SprocMapper.GetParameters(myModel, UpdateProcedureName);
```

### Custom aggregation
By default SprocMapper assumes that when aggregated, the entities should be turned into strings and joined with commas: `objects => string.Join(",", objects)`. If you require a custom operation (use semi-colon or do something completely different) you cane specify it during creating map:
```
	SprocMapper.CreateMap<MyModelClass>(UpdateProcedureName)
		.GroupBy(m => m.Value, opt => opt.AsParameter("value_param"))
		.Aggregate(m => m.Id, opt => opt.AsParameter("id_list").SetAggregationStrategy(objects => string.Join(";", objects.Cast<int>().Select(i=>ProcessNumericalId(i));
```

This for example could result in obtaining a parameter like: `@id_list = 'A001;A002;A003'`