using System;
using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.Models
{
    /// <summary>
    /// Result returned after processing overdue contracts
    /// </summary>
    public class ProcessOverdueContractsResult
    {
        /// <summary>
        /// Total number of present month contracts found
        /// </summary>
        public int TotalPresentMonthContracts { get; set; }

        /// <summary>
        /// Number of contracts identified as overdue
        /// </summary>
        public int OverdueCount { get; set; }

        /// <summary>
        /// Number of contracts successfully inserted into CE_OverDueContracts
        /// </summary>
        public int InsertedCount { get; set; }

        /// <summary>
        /// Timestamp when processing occurred
        /// </summary>
        public DateTime ProcessedAt { get; set; }

        /// <summary>
        /// List of EIds that were marked as overdue
        /// </summary>
        public List<string> OverdueEIds { get; set; }
    }
}

