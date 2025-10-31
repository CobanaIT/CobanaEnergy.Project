using CobanaEnergy.Project.Controllers;
using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Service.UserService;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Microsoft.Owin.Security.Cookies;
using Owin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

[assembly: OwinStartup(typeof(CobanaEnergy.Project.Startup))]

namespace CobanaEnergy.Project
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            app.CreatePerOwinContext<ApplicationDBContext>(ApplicationDBContext.Create);
            // Existing code remains unchanged
            app.CreatePerOwinContext<UserManager<ApplicationUser>>(AccountController.CreateUserManager);

            app.CreatePerOwinContext<RoleManager<IdentityRole>>(
                (options, context) => new RoleManager<IdentityRole>(
                    new RoleStore<IdentityRole>(context.Get<ApplicationDBContext>())));

            app.UseCookieAuthentication(new Microsoft.Owin.Security.Cookies.CookieAuthenticationOptions
            {
                AuthenticationType = Microsoft.AspNet.Identity.DefaultAuthenticationTypes.ApplicationCookie,
                LoginPath = new PathString("/Account/Login"),
                ExpireTimeSpan = TimeSpan.FromHours(24),
                SlidingExpiration = true, // session will be extended if the user is active
                                          //CookieSecure = CookieSecureOption.Always, // Only send cookie over HTTPS
                Provider = new CookieAuthenticationProvider
                {
                    // security stamp validation
                    OnValidateIdentity = SecurityStampValidator.OnValidateIdentity<UserManager<ApplicationUser>, ApplicationUser>(
                    validateInterval: TimeSpan.FromMinutes(30),
                    regenerateIdentity: (manager, user) => manager.CreateIdentityAsync(user, DefaultAuthenticationTypes.ApplicationCookie))
                }
            });
            // SignalR mapping - must be last
            GlobalHost.DependencyResolver.Register(typeof(IUserIdProvider), () => new CustomUserIdProvider());
            app.MapSignalR();

        }
    }
}