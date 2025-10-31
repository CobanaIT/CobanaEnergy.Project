using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector.Brokerage
{
    public class BrokerageStaffViewModel
    {
        [Required]
        [Display(Name = "Brokerage Staff Name")]
        public string BrokerageStaffName { get; set; }

        [Required]
        [Display(Name = "Active")]
        public bool Active { get; set; }

        [Display(Name = "Start Date")]
        [DataType(DataType.Date)]
        public string StartDate { get; set; }

        [Display(Name = "End Date")]
        [DataType(DataType.Date)]
        public string EndDate { get; set; }

        [EmailAddress]
        [Display(Name = "Email")]
        public string Email { get; set; }

        [Display(Name = "Landline")]
        public string Landline { get; set; }

        [Display(Name = "Mobile")]
        public string Mobile { get; set; }

        public string Notes { get; set; }
    }
}
