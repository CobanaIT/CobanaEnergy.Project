using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;

namespace CobanaEnergy.BackgroundServices
{
    public class RouteConfig
    {
        public static void RegisterRoutes(RouteCollection routes)
        {
            routes.IgnoreRoute("{resource}.axd/{*pathInfo}");

            // Health check endpoint at root path
            routes.MapRoute(
                name: "HealthCheck",
                url: "",
                defaults: new { controller = "Health", action = "Index" },
                namespaces: new[] { "CobanaEnergy.BackgroundServices.Controllers" }
            );
        }
    }
}
