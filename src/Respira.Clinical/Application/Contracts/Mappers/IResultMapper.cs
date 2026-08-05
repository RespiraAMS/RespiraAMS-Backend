namespace Application.Contracts.Mappers;

public interface IResultMapper<in TModel, out TResult>
{
    /// <summary>
    /// This method is best used for in memory mapping. When using for SQL query, it may cause
    /// an exception depend on how you use it. <br />
    ///- If the mapping is use on a top-level projection (essentially, the last call to Select()),
    /// then the mapping can work <br />
    /// - If not, then the mapping will likely cause an exception to be thrown <br />
    /// For more information, refer: https://learn.microsoft.com/en-us/ef/core/querying/client-eval <br />
    /// </summary>
    TResult ToResult(TModel model);
}