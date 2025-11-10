using System;
using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Command to process objection date contracts and update status to "Objection Closed"
    /// Contracts are processed where objectionDate + 1 day = CurrentDate
    /// </summary>
    public class ProcessObjectionDateCommand : ICommand<ProcessObjectionDateResult>
    {
        /// <summary>
        /// The current date to compare against objection dates
        /// </summary>
        public DateTime CurrentDate { get; set; }

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

