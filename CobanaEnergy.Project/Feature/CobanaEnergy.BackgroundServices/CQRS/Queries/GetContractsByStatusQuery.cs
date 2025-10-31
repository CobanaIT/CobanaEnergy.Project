using System;
using System.Collections.Generic;

namespace CobanaEnergy.BackgroundServices.CQRS.Queries
{
    /// <summary>
    /// Reusable query to get contracts filtered by status
    /// Can be used across multiple handlers and controllers
    /// </summary>
    public class GetContractsByStatusQuery : IQuery<List<ContractBasicInfo>>
    {
        /// <summary>
        /// Contract status to filter by (e.g., "Processing_Future Months", "Processing_Present Month")
        /// </summary>
        public string ContractStatus { get; set; }

        /// <summary>
        /// Optional: Additional filters for contract types
        /// </summary>
        public List<string> ContractTypes { get; set; }

        /// <summary>
        /// Optional: Filter by date range
        /// </summary>
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
    }

    /// <summary>
    /// Lightweight DTO for contract basic information
    /// Used across multiple queries and handlers
    /// </summary>
    public class ContractBasicInfo
    {
        public string EId { get; set; }
        public string Type { get; set; }
        public string ContractStatus { get; set; }
        public DateTime ModifyDate { get; set; }
        public DateTime? PostSalesCreationDate { get; set; }
    }
}


