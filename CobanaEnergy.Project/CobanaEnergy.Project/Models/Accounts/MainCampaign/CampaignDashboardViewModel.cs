using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Models.Accounts.MainCampaign
{
    public class CampaignDashboardViewModel
    {
        public int Count { get; set; }
        public int TotalBonus { get; set; }
        public bool TargetAchieved { get; set; }
        public long? SupplierId { get; set; }
        public Guid CampaignId { get; set; }
        public List<SelectListItem> Suppliers { get; set; } = new List<SelectListItem>();
        public List<CampaignDashboardRowViewModel> Campaigns { get; set; } = new List<CampaignDashboardRowViewModel>();
    }

    public class CampaignDashboardRowViewModel
    {
        public long Id { get; set; }
        public string BusinessName { get; set; }
        public string Number { get; set; }
        public string CreatedAt { get; set; }
        public string Bonus { get; set; }
    }
}