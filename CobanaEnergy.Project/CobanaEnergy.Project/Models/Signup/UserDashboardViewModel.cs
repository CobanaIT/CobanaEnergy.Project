using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Signup
{
    public class UserDashboardViewModel
    {
        public UserDashboardViewModel()
        {
            this.Roles = new List<string>();
        }
        public string UserName { get; set; }
        public string UserId { get; set; }
        public List<string> Roles { get; set; }
        public string Status => Enabled ? "Active" : "Inactive";
        public string OnlineStatus { get; set; } = "Offline"; // default
        public bool Enabled { get; set; }
    }
}