using Autofac;
using CobanaEnergy.Project.Controllers.Error;
using CobanaEnergy.Project.Filters;
using CobanaEnergy.Project.Service.BackgroundServices;
using System;
using System.Threading;
using System.Web;
using System.Web.Http;
using System.Web.Mvc;
using System.Web.Routing;

namespace CobanaEnergy.Project
{
    public class Global : HttpApplication
    {
        void Application_Start(object sender, EventArgs e)
        {
            // Code that runs on application startup
            AreaRegistration.RegisterAllAreas();
            GlobalConfiguration.Configure(WebApiConfig.Register);
            RouteConfig.RegisterRoutes(RouteTable.Routes);

            //DI
            AutofacConfig.RegisterDependencies();

            // Start background service
            CampaignMonitorRunner.Start();
            UserSessionMonitorRunner.Start();
        }

        protected void Application_Error()
        {
            Exception exception = Server.GetLastError();
            Response.Clear();

            var httpException = exception as HttpException;
            var routeData = new RouteData();
            routeData.Values["controller"] = "Error";

            if (httpException == null)
            {
                routeData.Values["action"] = "ServerError";
            }
            else
            {
                switch (httpException.GetHttpCode())
                {
                    case 404:
                        routeData.Values["action"] = "NotFound";
                        break;
                    default:
                        routeData.Values["action"] = "ServerError";
                        break;
                }
            }

            Server.ClearError();
            Response.TrySkipIisCustomErrors = true;

            IController controller = new ErrorController();
            controller.Execute(new RequestContext(new HttpContextWrapper(Context), routeData));
        }
    }
}