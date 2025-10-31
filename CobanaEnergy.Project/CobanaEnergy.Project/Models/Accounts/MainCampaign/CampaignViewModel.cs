using CobanaEnergy.Project.Models.Supplier.SupplierDBModels;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CobanaEnergy.Project.Models.Accounts.MainCampaign
{
    public class CampaignViewModel
    {
        [Key]
        public long Id { get; set; }

        [Required(ErrorMessage = "Campaign Name is required.")]
        [MaxLength(250)]
        public string CampaignName { get; set; }

        [Required]
        public long SupplierId { get; set; }

        public long? ProductId { get; set; }

        [Required]
        public DateTime StartDate { get; set; }

        [Required]
        public DateTime EndDate { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Sale Target must contain only digits.")]
        [MaxLength(250)]
        public string SaleTarget { get; set; }

        [Required]
        [RegularExpression(@"^[0-9]+$", ErrorMessage = "Bonus must contain only digits.")]
        [MaxLength(250)]
        public string Bonus { get; set; }

    }
}
