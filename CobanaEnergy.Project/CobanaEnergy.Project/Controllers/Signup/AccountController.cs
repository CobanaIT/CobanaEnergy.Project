using CobanaEnergy.Project.Controllers.Base;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.AwaitingPaymentsDashboard;
using CobanaEnergy.Project.Models.Accounts.MainCampaign;
using CobanaEnergy.Project.Models.Signup;
using CobanaEnergy.Project.Service.UserService;
using Logic;
using Logic.ResponseModel.Helper;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.UI.WebControls;

namespace CobanaEnergy.Project.Controllers
{
    /// <summary>
    /// Handles User Creation, Change Password and Login functinality
    /// </summary>
    public class AccountController : BaseController
    {
        private UserManager<ApplicationUser> _userManager => HttpContext.GetOwinContext().GetUserManager<UserManager<ApplicationUser>>();
        private RoleManager<IdentityRole> _roleManager => HttpContext.GetOwinContext().Get<RoleManager<IdentityRole>>();

        #region user_creation

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> Create()
        {
            try
            {
                ViewBag.Roles = await _roleManager.Roles.ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load roles: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Sign up Page rendering failed!! {ex.Message}");
                return RedirectToAction("Index", "Login");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Create(RegisterUserDto dto)
        {
            try
            {
                if (string.IsNullOrEmpty(dto.Username) || string.IsNullOrEmpty(dto.Password) || string.IsNullOrEmpty(dto.ConfirmPassword)
                    || dto.Roles.Count < 1)
                {
                    // return Json(new { success = false, message = "All Fields are Mandatory!" });
                    return JsonResponse.Fail("All Fields are Mandatory!");
                }
                if (dto.Password != dto.ConfirmPassword)
                {
                    return JsonResponse.Fail("Passwords & Confirm Password do not match.");
                    //return Json(new { success = false, message = "Passwords & Confirm Password do not match." });
                }

                foreach (var role in dto.Roles)
                {
                    var roleExists = await _roleManager.RoleExistsAsync(role);
                    if (!roleExists)
                    {
                        return JsonResponse.Fail($"Role '{role}' does not exist.");
                        // return Json(new { success = false, message = $"Role '{role}' does not exist." });
                    }
                }

                var usernamePattern = @"^[A-Za-z]{3,}$";
                if (!Regex.IsMatch(dto.Username, usernamePattern))
                {
                    return JsonResponse.Fail("Username must be at least 3 letters and contain only English alphabets.");
                }

                var user = new ApplicationUser { UserName = dto.Username, Email = $"{dto.Username}_{Guid.NewGuid()}@noemail.cobana", HasTimeRestriction = dto.HasTimeRestriction, Enabled = true };

                var result = await _userManager.CreateAsync(user, dto.Password);

                if (result.Succeeded)
                {
                    foreach (var role in dto.Roles)
                    {
                        await _userManager.AddToRoleAsync(user.Id, role);
                    }
                    //  return Json(new { success = true });
                    //return JsonResponse.Ok(new { UserId = user.Id }, "User created successfully");
                    return JsonResponse.Ok(new { success = true }, "");
                }

                var errorMessages = string.Join(", ", result.Errors);
                //return Json(new { success = false, message = errorMessages });
                return JsonResponse.Fail(errorMessages);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"User Creation Failed!! {ex.Message}");
                //return Json(new { success = false, message = $"An error occurred: {ex.Message}" });
                return JsonResponse.Fail($"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region change_password

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> ChangePassword()
        {
            try
            {
                ViewBag.Users = await _userManager.Users.ToListAsync();
                return View();
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load users: {ex.Message}";
                TempData["ToastType"] = "error";
                return RedirectToAction("Create");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> ChangePassword(string userId, string newPassword)
        {
            try
            {
                if (string.IsNullOrEmpty(userId) || string.IsNullOrEmpty(newPassword))
                {
                    return JsonResponse.Fail("All Fields are Mandatory!");
                }
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return JsonResponse.Fail("User not found.");

                var token = await _userManager.GeneratePasswordResetTokenAsync(user.Id);
                var result = await _userManager.ResetPasswordAsync(user.Id, token, newPassword);

                if (result.Succeeded)
                {
                    await _userManager.UpdateSecurityStampAsync(user.Id);
                    return JsonResponse.Ok(null, "Password updated successfully!");
                }

                var error = string.Join(", ", result.Errors);
                return JsonResponse.Fail(error);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Password change failed: {ex.Message}");
                return JsonResponse.Fail("Something went wrong while changing the password.");
            }
        }

        #endregion

        #region Login

        [HttpGet]
        [AllowAnonymous]
        public ActionResult Login()
        {
            if (User.Identity.IsAuthenticated)//// Update the View Method as well; FOR ANY NEW ROLES REDIRECTION
            {
                if (User.IsInRole("Pre-sales") || User.IsInRole("Controls"))
                    return RedirectToAction("Dashboard", "PreSales");
                else
                    return RedirectToAction("Index", "Home");
            }
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> Login(string username, string password)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
                    return JsonResponse.Fail("All fields are required.");

                var usernamePattern = @"^[A-Za-z]{3,}$";
                if (!Regex.IsMatch(username, usernamePattern))
                {
                    return JsonResponse.Fail("Username must be at least 3 letters and contain only English alphabets.");
                }

                var user = await _userManager.FindAsync(username, password);
                if (user == null || user.UserName != username)
                    return JsonResponse.Fail("Invalid username or password.");

                #region [Time Restriction]
                if (!user.Enabled)
                {
                    return JsonResponse.Fail("🚫 Your account has been deactivated. Please contact the administrator for assistance.");
                }
                #endregion

                #region [Time Restriction]
                if (user.HasTimeRestriction)
                {
                    var now = DateTime.Now.TimeOfDay;
                    if (now < TimeSpan.FromHours(8.5) || now > TimeSpan.FromHours(18.5))
                    {
                        return JsonResponse.Fail("You can only log in between 8:30 AM and 6:30 PM.");
                    }
                }
                #endregion



                var identity = await _userManager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie);
                var authManager = HttpContext.GetOwinContext().Authentication;
                authManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
                authManager.SignIn(new Microsoft.Owin.Security.AuthenticationProperties { IsPersistent = false }, identity);

                // Fetch user roles
                var roles = await _userManager.GetRolesAsync(user.Id);

                // Default redirect URL //// Update the View Method as well;
                string redirectUrl = Url.Action("Login", "Account");

                if (roles.Contains("Pre-sales"))
                {
                    redirectUrl = Url.Action("Dashboard", "PreSales");
                }
                else if (roles.Contains("Controls"))
                {
                    redirectUrl = Url.Action("Dashboard", "PreSales");
                }
                else
                {
                    redirectUrl = Url.Action("Index", "Home");
                }


                //return JsonResponse.Ok(null, "Login successful!");
                return JsonResponse.Ok(new { redirectUrl });
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Login failed for {username}: {ex.Message}");
                return JsonResponse.Fail("An error occurred while processing login.");
            }
        }

        #endregion

        #region logout

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Logout()
        {
            Request.GetOwinContext().Authentication.SignOut();
            Session.Clear();

            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetExpires(DateTime.UtcNow.AddDays(-1));
            Response.Cache.SetNoStore();

            return RedirectToAction("Login", "Account");
        }

        #endregion

        #region user_manager

        public static UserManager<ApplicationUser> CreateUserManager(IdentityFactoryOptions<UserManager<ApplicationUser>> options, IOwinContext context)
        {
            var manager = new UserManager<ApplicationUser>(new UserStore<ApplicationUser>(context.Get<ApplicationDBContext>()));
            manager.UserTokenProvider = new DataProtectorTokenProvider<ApplicationUser>(
                new Microsoft.Owin.Security.DataProtection.DpapiDataProtectionProvider("CobanaEnergy").Create("ASP.NET Identity"));
            return manager;
        }

        #endregion


        #region [user dashboard]

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public ActionResult Index()
        {
            return View("~/Views/Account/UserDashboard.cshtml", new UserDashboardViewModel());
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetUserList()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var result = users.Select(u => new UserDashboardViewModel
                {
                    UserName = u.UserName,
                    UserId = u.Id,
                    Roles = _userManager.GetRoles(u.Id).ToList(),
                    Enabled = u.Enabled,
                    OnlineStatus = ConnectedUserStore.IsUserConnected(u.Id) ? "Online" : "Offline"
                }).ToList();
                return JsonResponse.Ok(result);
            }
            catch (Exception ex)
            {
                Logger.Log("GetUserList: " + ex);
                return JsonResponse.Fail("Error fetching user data.");
            }
        }


        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateUsers(List<UserUpdateModel> users)
        {
            try
            {
                if (users == null || !users.Any())
                    return JsonResponse.Fail("No users selected.");

                var ids = users.Select(u => u.UserId).ToList();
                foreach (var userId in ids)
                {
                    var user = await _userManager.FindByIdAsync(userId);
                    if (user != null)
                    {
                        user.Enabled = !user.Enabled;
                        await _userManager.UpdateAsync(user);
                    }
                }

                return JsonResponse.Ok(message: "Users updated.");
            }
            catch (Exception ex)
            {
                Logger.Log("UpdateUsers: " + ex);
                return JsonResponse.Fail("Error updating Users.");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> GetUserDetails(string userId)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return JsonResponse.Fail("User not found.");
                var userRoles = await _userManager.GetRolesAsync(user.Id);
                var allRoles = _roleManager.Roles.Select(r => r.Name).ToList();
                var result = new
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Roles = userRoles,
                    AllRoles = allRoles
                };
                return JsonResponse.Ok(result);
            }
            catch (Exception ex)
            {
                Logger.Log("UpdateUsers: " + ex);
                return JsonResponse.Fail("Error updating Users.");
            }
        }


        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateJsonAntiForgeryToken]
        public async Task<JsonResult> UpdateUserRoles(string userId, List<string> roles)
        {
            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    return JsonResponse.Fail("User not found.");

                // Get current roles
                var currentRoles = await _userManager.GetRolesAsync(user.Id);

                foreach (var role in currentRoles)
                {
                    var removeResult = await _userManager.RemoveFromRoleAsync(user.Id,role);
                }

                foreach (var role in roles)
                {
                    var addResult = await _userManager.AddToRolesAsync(user.Id, role);
                }
                await _userManager.UpdateAsync(user);

                return JsonResponse.Ok("User roles updated successfully.");
            }
            catch (Exception ex)
            {
                Logger.Log("UpdateUserRoles: " + ex);
                return JsonResponse.Fail("Error updating user roles.");
            }
        }



        #endregion

        #region UserProfile

        [HttpGet]
        //[Authorize(Roles = "Controls")]
        public async Task<ActionResult> UserProfile()
        {
            try
            {
                var userId = User.Identity.GetUserId();
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    TempData["ToastMessage"] = "User not found.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("Login", "Account");
                }

                var model = new UserProfileViewModel
                {
                    UserName = user.UserName,
                    Email = user.Email,
                    JobTitle = user.JobTitle,
                    ExtensionNumber = user.ExtensionNumber
                };

                return View(model);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load profile: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"Profile page rendering failed!! {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        [HttpPost]
        //[Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateProfile(UserProfileViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                                 .Where(x => x.Value.Errors.Count > 0)
                                 .SelectMany(kvp => kvp.Value.Errors)
                                 .Select(e => e.ErrorMessage)
                                 .ToList();

                    string combinedError = string.Join("<br>", errors);
                    return JsonResponse.Fail(combinedError);
                }

                var userId = User.Identity.GetUserId();
                var user = await _userManager.FindByIdAsync(userId);

                if (user == null)
                {
                    return JsonResponse.Fail("User not found.");
                }

                // Update user properties
                user.Email = model.Email;
                user.JobTitle = model.JobTitle;
                user.ExtensionNumber = model.ExtensionNumber;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return JsonResponse.Ok(new { redirectUrl = Url.Action("Profile", "Account") }, "Profile updated successfully!");
                }

                var errorMessages = string.Join(", ", result.Errors);
                return JsonResponse.Fail(errorMessages);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"Profile update failed!! {ex.Message}");
                return JsonResponse.Fail($"An error occurred: {ex.Message}");
            }
        }

        #endregion

        #region UserProfileDashboard

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> UserProfileDashboard()
        {
            try
            {
                var users = await _userManager.Users.ToListAsync();
                var userList = users.Select(u => new UserProfileItemViewModel
                {
                    UserId = u.Id,
                    UserName = u.UserName,
                    JobTitle = u.JobTitle ?? "-",
                    ExtensionNumber = u.ExtensionNumber ?? "-",
                    Enabled = u.Enabled,
                    OnlineStatus = ConnectedUserStore.IsUserConnected(u.Id) ? "Online" : "Offline"
                }).ToList();

                var model = new UserProfileDashboardViewModel
                {
                    Users = userList,
                    TotalUsers = userList.Count,
                    ActiveUsers = userList.Count(u => u.Enabled),
                    OnlineUsers = userList.Count(u => u.OnlineStatus == "Online"),
                    CompleteProfiles = userList.Count(u => u.JobTitle != "-" || u.ExtensionNumber != "-")
                };

                return View("~/Views/Account/UserProfileDashboard.cshtml", model);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load user profile dashboard: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"UserProfileDashboard failed: {ex.Message}");
                return RedirectToAction("Index", "Home");
            }
        }

        #endregion

        #region UserUpdate

        [HttpGet]
        [Authorize(Roles = "Controls")]
        public async Task<ActionResult> UpdateUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
            {
                TempData["ToastMessage"] = "User ID is required.";
                TempData["ToastType"] = "error";
                return RedirectToAction("UserProfileDashboard", "Account");
            }

            try
            {
                var user = await _userManager.FindByIdAsync(userId);
                
                if (user == null)
                {
                    TempData["ToastMessage"] = "User not found.";
                    TempData["ToastType"] = "error";
                    return RedirectToAction("UserProfileDashboard", "Account");
                }

                var model = new UserUpdateViewModel
                {
                    UserId = user.Id,
                    UserName = user.UserName,
                    Email = user.Email,
                    JobTitle = user.JobTitle,
                    ExtensionNumber = user.ExtensionNumber
                };

                return View("~/Views/Account/UpdateUser.cshtml", model);
            }
            catch (Exception ex)
            {
                TempData["ToastMessage"] = $"Failed to load user: {ex.Message}";
                TempData["ToastType"] = "error";
                Logic.Logger.Log($"UpdateUser GET failed!! {ex.Message}");
                return RedirectToAction("UserProfileDashboard", "Account");
            }
        }

        [HttpPost]
        [Authorize(Roles = "Controls")]
        [ValidateAntiForgeryToken]
        public async Task<JsonResult> UpdateUser(UserUpdateViewModel model)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    var errors = ModelState
                                 .Where(x => x.Value.Errors.Count > 0)
                                 .SelectMany(kvp => kvp.Value.Errors)
                                 .Select(e => e.ErrorMessage)
                                 .ToList();

                    string combinedError = string.Join("<br>", errors);
                    return JsonResponse.Fail(combinedError);
                }

                var user = await _userManager.FindByIdAsync(model.UserId);

                if (user == null)
                {
                    return JsonResponse.Fail("User not found.");
                }

                // Update user properties
                user.Email = model.Email;
                user.JobTitle = model.JobTitle;
                user.ExtensionNumber = model.ExtensionNumber;

                var result = await _userManager.UpdateAsync(user);

                if (result.Succeeded)
                {
                    return JsonResponse.Ok(new { redirectUrl = Url.Action("UserProfileDashboard", "Account") }, "User updated successfully!");
                }

                var errorMessages = string.Join(", ", result.Errors);
                return JsonResponse.Fail(errorMessages);
            }
            catch (Exception ex)
            {
                Logic.Logger.Log($"User update failed!! {ex.Message}");
                return JsonResponse.Fail($"An error occurred: {ex.Message}");
            }
        }

        #endregion

    }
}