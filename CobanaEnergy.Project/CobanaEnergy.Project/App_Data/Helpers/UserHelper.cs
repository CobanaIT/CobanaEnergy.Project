using CobanaEnergy.Project.Models;
using Logic;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using System;
using System.Threading.Tasks;
using System.Web;

namespace CobanaEnergy.Project.Helpers
{
    /// <summary>
    /// Helper methods for user-related operations.
    /// </summary>
    public static class UserHelper
    {
        /// <summary>
        /// Gets the username from a user ID using UserManager. 
        /// </summary>
        /// <param name="userId">The user ID to look up</param>
        /// <param name="httpContext">The HTTP context to get UserManager from</param>
        /// <returns>The username, or "Unknown User" if not found</returns>
        public static async Task<string> GetUsernameFromUserId(string userId, HttpContextBase httpContext)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(userId))
                    return "Unknown User";

                var userManager = httpContext.GetOwinContext().GetUserManager<UserManager<ApplicationUser>>();
                var user = await userManager.FindByIdAsync(userId);
                
                return user?.UserName ?? "Unknown User";
            }
            catch (Exception ex)
            {
                Logger.Log($"UserHelper: Error getting username for userId {userId}: {ex.Message}");
                return "Unknown User";
            }
        }
    }
}
