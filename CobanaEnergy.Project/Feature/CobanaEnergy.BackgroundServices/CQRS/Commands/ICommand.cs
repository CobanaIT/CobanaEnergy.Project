namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Marker interface for commands (Write operations)
    /// Commands represent the intent to change state and may return a result
    /// </summary>
    /// <typeparam name="TResult">The result type returned by the command</typeparam>
    public interface ICommand<TResult>
    {
    }
}


