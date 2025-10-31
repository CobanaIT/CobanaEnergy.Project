using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Web.Routing;
using System.Web.Security;
using System.Web.SessionState;
using System.Web.Http;
using CobanaEnergy.BackgroundServices.App_Start;

namespace CobanaEnergy.BackgroundServices
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            
            // Configure Web API
            GlobalConfiguration.Configure(WebApiConfig.Register);
            
            // Configure Dependency Injection (Autofac)
            GlobalConfiguration.Configure(AutofacConfig.Register);
            
            // Configure MVC routes
            RouteConfig.RegisterRoutes(RouteTable.Routes);            
        }
    }
}