using System;
using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.Models
{
    /// <summary>
    /// Result returned after processing renewal window date contracts
    /// </summary>
    public class ProcessRenewalWindowDateResult
    {
        /// <summary>
        /// Total number of contracts with "Live" status found
        /// </summary>
        public int TotalLiveContracts { get; set; }

        /// <summary>
        /// Number of contracts where CED is within 180 days
        /// </summary>
        public int MatchedRenewalWindowCount { get; set; }

        /// <summary>
        /// Number of contracts successfully updated to "Renewal Window"
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Timestamp when processing occurred
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// List of EIds that were updated to "Renewal Window"
        /// </summary>
        public List<string> UpdatedEIds { get; set; }
    }
}

