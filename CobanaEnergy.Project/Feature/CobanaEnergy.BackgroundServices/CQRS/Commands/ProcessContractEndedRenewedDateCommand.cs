using System;
using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Command to process contract ended Renewed date contracts
    /// Updates contracts from "Renewed" to "Contract Ended - Renewed" where CED == CurrentDate
    /// </summary>
    public class ProcessContractEndedRenewedDateCommand : ICommand<ProcessContractEndedDateResult>
    {
        /// <summary>
        /// The current date to compare against CED
        /// </summary>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Source status to filter by (defaults to "Renewed")
        /// </summary>
        public string SourceStatus { get; set; } = "Renewed";

        /// <summary>
        /// Target status to set (defaults to "Contract Ended - Renewed")
        /// </summary>
        public string TargetStatus { get; set; } = "Contract Ended - Renewed";
    }
}

