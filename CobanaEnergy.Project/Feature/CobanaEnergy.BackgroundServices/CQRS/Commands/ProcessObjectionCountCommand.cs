using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Command to process objection count contracts and update status to "Objection Closed"
    /// Contracts are updated where ObjectionCount == MaxObjectionCount for the supplier
    /// </summary>
    public class ProcessObjectionCountCommand : ICommand<ProcessObjectionCountResult>
    {
        /// <summary>
        /// Source status to filter by (defaults to "Objection")
        /// </summary>
        public string SourceStatus { get; set; } = "Objection";

        /// <summary>
        /// Target status to set (defaults to "Objection Closed")
        /// </summary>
        public string TargetStatus { get; set; } = "Objection Closed";
    }
}



