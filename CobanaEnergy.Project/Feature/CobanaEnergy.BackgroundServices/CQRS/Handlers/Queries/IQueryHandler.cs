using System.Threading.Tasks;
using CobanaEnergy.BackgroundServices.CQRS.Queries;

namespace CobanaEnergy.BackgroundServices.CQRS.Handlers.Queries
{
    /// <summary>
    /// Generic interface for query handlers (Read operations)
    /// Query handlers should be read-only and not modify any state
    /// </summary>
    /// <typeparam name="TQuery">The query type to handle</typeparam>
    /// <typeparam name="TResult">The result type returned by the query</typeparam>
    public interface IQueryHandler<TQuery, TResult> where TQuery : IQuery<TResult>
    {
        /// <summary>
        /// Handles the query execution asynchronously
        /// </summary>
        /// <param name="query">The query to execute</param>
        /// <returns>The query result</returns>
        Task<TResult> HandleAsync(TQuery query);
    }
}


