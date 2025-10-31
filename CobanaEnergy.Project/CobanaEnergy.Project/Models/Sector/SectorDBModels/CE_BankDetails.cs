using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Sector.SectorDBModels
{
    [Table("CE_BankDetails")]
    public class CE_BankDetails
    {
        [Key]
        public int BankDetailsID { get; set; }

        [Required]
        [StringLength(50)]
        public string EntityType { get; set; }

        [Required]
        public int EntityID { get; set; }

        [StringLength(200)]
        public string BankName { get; set; }

        [StringLength(300)]
        public string BankBranchAddress { get; set; }

        [StringLength(300)]
        public string ReceiversAddress { get; set; }

        [StringLength(200)]
        public string AccountName { get; set; }

        [StringLength(50)]
        public string AccountSortCode { get; set; }

        [StringLength(50)]
        public string AccountNumber { get; set; }

        [StringLength(50)]
        public string IBAN { get; set; }

        [StringLength(50)]
        public string SwiftCode { get; set; }

        // Navigation Properties - None needed for polymorphic relationships
        // Relationships are handled manually using EntityType and EntityID
    }
}