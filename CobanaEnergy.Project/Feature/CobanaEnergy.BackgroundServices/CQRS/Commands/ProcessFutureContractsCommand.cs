using System;
using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Command for processing future contracts to present month status
    /// </summary>
    public class ProcessFutureContractsCommand : ICommand<ProcessContractsResult>
    {
        /// <summary>
        /// The date to use for month comparison (typically DateTime.Now)
        /// </summary>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// The target status to set (e.g., "Processing_Present Month")
        /// </summary>
        public string TargetStatus { get; set; }

        /// <summary>
        /// Optional: Source status to filter by (defaults to "Processing_Future Months")
        /// </summary>
        public string SourceStatus { get; set; } = "Processing_Future Months";
    }
}

