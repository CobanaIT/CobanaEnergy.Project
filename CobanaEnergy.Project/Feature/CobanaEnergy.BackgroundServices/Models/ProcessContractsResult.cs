using System;
using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.Models
{
    /// <summary>
    /// Result object returned by contract processing operations
    /// </summary>
    public class ProcessContractsResult
    {
        /// <summary>
        /// Number of contracts successfully updated
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Total number of future contracts found
        /// </summary>
        public int TotalFutureContracts { get; set; }

        /// <summary>
        /// Number of contracts that matched the current month criteria
        /// </summary>
        public int MatchedCurrentMonth { get; set; }

        /// <summary>
        /// Processing timestamp
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// List of EIds that were updated
        /// </summary>
        public List<string> UpdatedEIds { get; set; }
    }
}

