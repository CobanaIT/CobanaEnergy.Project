using System;
using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.Models
{
    /// <summary>
    /// Result returned after processing contract ended date contracts
    /// </summary>
    public class ProcessContractEndedDateResult
    {
        /// <summary>
        /// Total number of contracts with source status found
        /// </summary>
        public int TotalSourceContracts { get; set; }

        /// <summary>
        /// Number of contracts where CED == CurrentDate
        /// </summary>
        public int MatchedDateCount { get; set; }

        /// <summary>
        /// Number of contracts successfully updated to target status
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Timestamp when processing occurred
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// List of EIds that were updated
        /// </summary>
        public List<string> UpdatedEIds { get; set; }
    }
}

