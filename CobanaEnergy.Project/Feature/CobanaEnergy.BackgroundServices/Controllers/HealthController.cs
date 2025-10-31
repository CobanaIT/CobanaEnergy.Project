using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace CobanaEnergy.BackgroundServices.Controllers
{
    public class HealthController : Controller
    {
        // GET: / (Health check endpoint)
        public ActionResult Index()
        {
            var uptime = DateTime.Now - System.Diagnostics.Process.GetCurrentProcess().StartTime;
            
            return Content($@"
                <html>
                <head>
                    <title>Cobana Energy Background Services - Health Check</title>
                    <style>
                        body {{ font-family: Arial, sans-serif; padding: 40px; background: #f5f5f5; }}
                        .container {{ background: white; padding: 30px; border-radius: 8px; box-shadow: 0 2px 4px rgba(0,0,0,0.1); max-width: 600px; }}
                        .status {{ color: #28a745; font-size: 24px; font-weight: bold; }}
                        .info {{ margin: 20px 0; padding: 15px; background: #f8f9fa; border-left: 4px solid #007bff; }}
                        .timestamp {{ color: #666; font-size: 14px; }}
                        a {{ color: #007bff; text-decoration: none; }}
                        a:hover {{ text-decoration: underline; }}
                    </style>
                </head>
                <body>
                    <div class='container'>
                        <h1>🏥 Cobana Energy Background Services</h1>
                        <div class='status'>✅ Service Status: RUNNING</div>
                        
                        <div class='info'>
                            <strong>Service Information:</strong><br/>
                            • Uptime: {uptime.Days}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s<br/>
                            • Server Time: {DateTime.Now:yyyy-MM-dd HH:mm:ss}<br/>
                            • Environment: {Environment.MachineName}
                        </div>
                        
                        <div class='info'>
                            <strong>API Endpoints:</strong><br/>
                            • Base API: <a href='/api/DefaultWebApi'>/api/DefaultWebApi</a><br/>
                            • Health Check: <a href='/'>/</a> (this page)
                        </div>
                        
                        <div class='timestamp'>
                            Last checked: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC
                        </div>
                    </div>
                </body>
                </html>
            ", "text/html");
        }
    }
}

