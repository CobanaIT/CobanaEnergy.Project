using Autofac;
using CobanaEnergy.Project.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace CobanaEnergy.Project.Service.BackgroundServices
{
    public class UserSessionMonitorRunner
    {
        private static Timer _campaignMonitorTimer;
        private static bool _isRunning = false;

        public static void Start()
        {
            _campaignMonitorTimer = new Timer(async state =>
            {
                if (_isRunning) return;
                _isRunning = true;

                try
                {
                    using (var scope = AutofacConfig.Container.BeginLifetimeScope())
                    {
                        var monitor = scope.Resolve<UserSessionMonitorService>();
                        await monitor.CheckUserSession();
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception (e.g., using log4net, Serilog, etc.)
                    Console.WriteLine("User Session Monitor error: " + ex.Message);
                }
                finally
                {
                    _isRunning = false;
                }
            }, null, TimeSpan.Zero, TimeSpan.FromMinutes(1)); // Run every minute
        }
    }
}