using System;
using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.Models
{
    /// <summary>
    /// Result returned after processing objection date contracts
    /// </summary>
    public class ProcessObjectionDateResult
    {
        /// <summary>
        /// Total number of contracts with "Objection" status found
        /// </summary>
        public int TotalObjectionContracts { get; set; }

        /// <summary>
        /// Number of contracts where objectionDate + 1 day = CurrentDate
        /// </summary>
        public int MatchedObjectionDateCount { get; set; }

        /// <summary>
        /// Number of contracts successfully updated to "Objection Closed"
        /// </summary>
        public int UpdatedCount { get; set; }

        /// <summary>
        /// Timestamp when processing occurred
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// List of EIds that were updated to "Objection Closed"
        /// </summary>
        public List<string> UpdatedEIds { get; set; }
    }
}

