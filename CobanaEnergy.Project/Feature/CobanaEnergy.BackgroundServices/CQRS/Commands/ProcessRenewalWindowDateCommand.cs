using System;
using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Command to process renewal window date contracts
    /// Updates contracts from "Live" to "Renewal Window" where CED is within 180 days
    /// </summary>
    public class ProcessRenewalWindowDateCommand : ICommand<ProcessRenewalWindowDateResult>
    {
        /// <summary>
        /// The current date to compare against CED
        /// </summary>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Source status to filter by (defaults to "Live")
        /// </summary>
        public string SourceStatus { get; set; } = "Live";

        /// <summary>
        /// Target status to set (defaults to "Renewal Window")
        /// </summary>
        public string TargetStatus { get; set; } = "Renewal Window";

        /// <summary>
        /// Number of days threshold for renewal window (defaults to 180)
        /// </summary>
        public int DaysThreshold { get; set; } = 180;
    }
}

