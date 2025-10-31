using System;
using CobanaEnergy.BackgroundServices.Models;

namespace CobanaEnergy.BackgroundServices.CQRS.Commands
{
    /// <summary>
    /// Command to process present month contracts and identify overdue ones
    /// Contracts are considered overdue if CurrentDate > StartDate/InitialStartDate
    /// </summary>
    public class ProcessOverdueContractsCommand : ICommand<ProcessOverdueContractsResult>
    {
        /// <summary>
        /// The current date to compare against start dates
        /// </summary>
        public DateTime CurrentDate { get; set; }

        /// <summary>
        /// Source status to filter by (defaults to "Processing_Present Month")
        /// </summary>
        public string SourceStatus { get; set; } = "Processing_Present Month";
    }
}


