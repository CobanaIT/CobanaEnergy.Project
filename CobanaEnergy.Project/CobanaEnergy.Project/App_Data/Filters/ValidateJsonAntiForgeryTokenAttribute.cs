using Logic;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Helpers;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Filters
{
    public class ValidateJsonAntiForgeryTokenAttribute : AuthorizeAttribute
    {
        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            if (filterContext == null)
                throw new ArgumentNullException("filterContext");

            try
            {
                var request = filterContext.HttpContext.Request;
                var cookieToken = request.Cookies["__RequestVerificationToken"]?.Value;
                var formToken = request.Headers["RequestVerificationToken"];

                AntiForgery.Validate(cookieToken, formToken);
            }
            catch (HttpAntiForgeryException ex)
            {
                Logger.Log("AntiForgery validation failed: " + ex.Message);
                filterContext.Result = new HttpStatusCodeResult(403, "Anti-forgery validation failed");
            }
        }
    }
}