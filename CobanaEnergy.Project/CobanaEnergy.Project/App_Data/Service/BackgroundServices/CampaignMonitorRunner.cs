using Autofac;
using CobanaEnergy.Project.Filters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Web;

namespace CobanaEnergy.Project.Service.BackgroundServices
{
    public static class CampaignMonitorRunner
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
                        var monitor = scope.Resolve<CampaignMonitorService>();
                        await monitor.CheckAndSendCampaignNotificationsAsync();
                    }
                }
                catch (Exception ex)
                {
                    // Log the exception (e.g., using log4net, Serilog, etc.)
                    Console.WriteLine("CampaignMonitor error: " + ex.Message);
                }
                finally
                {
                    _isRunning = false;
                }
            }, null, TimeSpan.Zero, TimeSpan.FromHours(1)); // Run every minute
        }
    }
}