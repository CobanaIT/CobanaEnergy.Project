using System;
using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Command to process contract ended Not Renewed date contracts
    /// Updates contracts from "Renewal Window" to "Contract Ended - Not Renewed" where CED == CurrentDate
    /// </summary>
    public class ProcessContractEndedNotRenewedDateCommand : ICommand<ProcessContractEndedDateResult>
    {
        /// <summary>
        /// The current date to compare against CED
        /// </summary>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Source status to filter by (defaults to "Renewal Window")
        /// </summary>
        public string SourceStatus { get; set; } = "Renewal Window";

        /// <summary>
        /// Target status to set (defaults to "Contract Ended - Not Renewed")
        /// </summary>
        public string TargetStatus { get; set; } = "Contract Ended - Not Renewed";
    }
}

