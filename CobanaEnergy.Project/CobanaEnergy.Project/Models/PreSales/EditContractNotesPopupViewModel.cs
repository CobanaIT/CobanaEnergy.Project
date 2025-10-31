using System;

namespace CobanaEnergy.Project.Models.PreSales
{
    public class EditContractNotesPopupViewModel
    {
        public string EId { get; set; }
        public string ContractType { get; set; }
        public string ContractNotes { get; set; }
        public string PreSalesFollowUpDate { get; set; }
        public bool IsDualContract { get; set; }
    }
}
