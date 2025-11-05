namespace CobanaEnergy.BackgroundServices.CQRS.Queries
{
    /// <summary>
    /// Reusable query to get Electric or Gas contract details by EId
    /// Handles both contract types in a single query
    /// </summary>
    public class GetContractDetailsByTypeQuery : IQuery<ContractDetailsDto>
    {
        public string EId { get; set; }
        public string ContractType { get; set; }  // "Electric" or "Gas"
    }

    /// <summary>
    /// DTO for contract details (Electric or Gas)
    /// </summary>
    public class ContractDetailsDto
    {
        public string EId { get; set; }
        public string ContractType { get; set; }
        public string InitialStartDate { get; set; }
        public string BusinessName { get; set; }
        public string MPAN_MPRN { get; set; }  // MPAN for Electric, MPRN for Gas
        public long SupplierId { get; set; }
        public bool Found { get; set; }
    }
}


