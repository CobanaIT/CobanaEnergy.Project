namespace CobanaEnergy.BackgroundServices.CQRS.Queries
{
    /// <summary>
    /// Reusable query to get commission and reconciliation record by EId
    /// </summary>
    public class GetCommissionRecordByEIdQuery : IQuery<CommissionRecordDto>
    {
        public string EId { get; set; }
    }

    /// <summary>
    /// DTO for commission and reconciliation record data
    /// </summary>
    public class CommissionRecordDto
    {
        public string EId { get; set; }
        public string StartDate { get; set; }
        public string CED { get; set; }
        public string CED_COT { get; set; }
        public bool Found { get; set; }
    }
}


