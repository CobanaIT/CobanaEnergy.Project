using System;
using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.Models
{
    /// <summary>
    /// Result returned after processing objection count contracts
    /// </summary>
    public class ProcessObjectionCountResult
    {
        /// <summary>
        /// Total number of contracts with "Objection" status found
        /// </summary>
        public int TotalObjectionContracts { get; set; }

        /// <summary>
        /// Number of contracts where ObjectionCount reached MaxObjectionCount
        /// </summary>
        public int MatchedMaxObjectionCount { get; set; }

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



