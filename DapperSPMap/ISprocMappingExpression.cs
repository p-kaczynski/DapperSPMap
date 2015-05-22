namespace DapperSPMap
{
    public interface ISprocMappingExpression<TModel> : ISingleSprocMappingExpression<TModel>,
        IMultipleSprocMappingExpression<TModel>
    {
    }
}