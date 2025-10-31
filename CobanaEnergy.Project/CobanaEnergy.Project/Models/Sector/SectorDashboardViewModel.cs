using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CobanaEnergy.Project.Models.Sector
{
    public class SectorDashboardViewModel
    {
        public List<SectorItemViewModel> Sectors { get; set; }
        public int TotalSectors { get; set; }
        public int ActiveSectors { get; set; }
        public int InactiveSectors { get; set; }
        public List<SectorTypeOption> SectorTypeOptions { get; set; }
        
        public SectorDashboardViewModel()
        {
            Sectors = new List<SectorItemViewModel>();
            SectorTypeOptions = new List<SectorTypeOption>
            {
                new SectorTypeOption { Value = "", Text = "All Sectors" },
                new SectorTypeOption { Value = "Brokerage", Text = "Brokerage" },
                new SectorTypeOption { Value = "Closer", Text = "Closer" },
                new SectorTypeOption { Value = "Leads Generator", Text = "Leads Generator" },
                new SectorTypeOption { Value = "Referral Partner", Text = "Referral Partner" },
                new SectorTypeOption { Value = "Introducer", Text = "Introducer" }
            };
        }
    }

    public class SectorItemViewModel
    {
        public string SectorId { get; set; }
        
        [Display(Name = "Name")]
        public string Name { get; set; }
        
        [Display(Name = "Active")]
        public bool Active { get; set; }
        
        [Display(Name = "Created Date")]
        public string CreatedDate { get; set; }
        
        [Display(Name = "Mobile")]
        public string Mobile { get; set; }
        
        [Display(Name = "Sector Type")]
        public string SectorType { get; set; }
        
        [Display(Name = "Suppliers")]
        public List<string> Suppliers { get; set; } = new List<string>();
    }
    
    public class SectorTypeOption
    {
        public string Value { get; set; }
        public string Text { get; set; }
    }
}
