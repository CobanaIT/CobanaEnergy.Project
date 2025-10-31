using System.Threading.Tasks;
using CobanaEnergy.BackgroundServices.CQRS.Commands;

namespace CobanaEnergy.BackgroundServices.CQRS.Handlers.Commands
{
    /// <summary>
    /// Generic interface for command handlers (Write operations)
    /// Each command has its own handler responsible for executing the command's logic
    /// </summary>
    /// <typeparam name="TCommand">The command type to handle</typeparam>
    /// <typeparam name="TResult">The result type returned by the command</typeparam>
    public interface ICommandHandler<TCommand, TResult> where TCommand : ICommand<TResult>
    {
        /// <summary>
        /// Handles the command execution asynchronously
        /// </summary>
        /// <param name="command">The command to execute</param>
        /// <returns>The result of the command execution</returns>
        Task<TResult> HandleAsync(TCommand command);
    }
}


