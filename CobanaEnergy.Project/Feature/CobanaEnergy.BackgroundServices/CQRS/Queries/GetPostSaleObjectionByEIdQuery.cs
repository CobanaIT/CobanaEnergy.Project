namespace CobanaEnergy.BackgroundServices.CQRS.Queries
{
    /// <summary>
    /// Reusable query to get PostSaleObjection record by EId
    /// Used to check if objection date is one day before current date
    /// </summary>
    public class GetPostSaleObjectionByEIdQuery : IQuery<PostSaleObjectionDto>
    {
        /// <summary>
        /// The EId (contract identifier) to search for
        /// </summary>
        public string EId { get; set; }

        /// <summary>
        /// Optional: Contract type to filter by
        /// </summary>
        public string ContractType { get; set; }
    }

    /// <summary>
    /// DTO for PostSaleObjection record data
    /// </summary>
    public class PostSaleObjectionDto
    {
        public string EId { get; set; }
        public string ContractType { get; set; }
        public string ObjectionDate { get; set; }
        public string QueryType { get; set; }
        public int ObjectionCount { get; set; }
        public bool Found { get; set; }
    }
}

