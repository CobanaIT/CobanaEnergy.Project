using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.Project.Controllers.Error
{
    public class ErrorController : Controller
    {
        public ActionResult NotFound()
        {
            Response.StatusCode = 404;
            return View("NotFound");
        }

        public ActionResult ServerError()
        {
            Response.StatusCode = 500;
            return View("Error");
        }
    }
}