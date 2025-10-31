using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Models.Signup
{
    public class UserProfileDashboardViewModel
    {
        public UserProfileDashboardViewModel()
        {
            Users = new List<UserProfileItemViewModel>();
        }

        public List<UserProfileItemViewModel> Users { get; set; }
        public int TotalUsers { get; set; }
        public int ActiveUsers { get; set; }
        public int OnlineUsers { get; set; }
        public int CompleteProfiles { get; set; }
    }

    public class UserProfileItemViewModel
    {
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string JobTitle { get; set; }
        public string ExtensionNumber { get; set; }
        public bool Enabled { get; set; }
        public string OnlineStatus { get; set; }
    }
}
