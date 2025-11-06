using System;
using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Command to process contract ended Ag Lost date contracts
    /// Updates contracts from "Renewal Window - Ag Lost" to "Contract Ended - Ag Lost" where CED == CurrentDate
    /// </summary>
    public class ProcessContractEndedAgLostDateCommand : ICommand<ProcessContractEndedDateResult>
    {
        /// <summary>
        /// The current date to compare against CED
        /// </summary>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Source status to filter by (defaults to "Renewal Window - Ag Lost")
        /// </summary>
        public string SourceStatus { get; set; } = "Renewal Window - Ag Lost";

        /// <summary>
        /// Target status to set (defaults to "Contract Ended - Ag Lost")
        /// </summary>
        public string TargetStatus { get; set; } = "Contract Ended - Ag Lost";
    }
}

