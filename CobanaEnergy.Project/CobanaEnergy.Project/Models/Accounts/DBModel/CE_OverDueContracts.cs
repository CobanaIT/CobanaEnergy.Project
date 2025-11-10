using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace CobanaEnergy.Project.Models.Accounts
{
    [Table("CE_OverDueContracts")]
    public class CE_OverDueContracts
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }

        [Required]
        public string EId { get; set; }

        [Required]
        public string Type { get; set; }

        public DateTime DetectedAsOverdueDate { get; set; }
    }
}

