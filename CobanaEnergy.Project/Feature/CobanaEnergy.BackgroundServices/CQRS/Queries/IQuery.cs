namespace CobanaEnergy.BackgroundServices.CQRS.Queries
{
    /// <summary>
    /// Marker interface for queries (Read operations)
    /// Queries should be read-only and return data without side effects
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the query</typeparam>
    public interface IQuery<TResult>
    {
    }
}


