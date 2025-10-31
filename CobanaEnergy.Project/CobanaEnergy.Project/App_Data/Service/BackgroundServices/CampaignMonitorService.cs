using CobanaEnergy.Project.Models;
using CobanaEnergy.Project.Models.Accounts.MainCampaign;
using CobanaEnergy.Project.Models.Accounts.MainCampaign.DBModel;
using CobanaEnergy.Project.Service.NotificationHub;
using CobanaEnergy.Project.Service.UserService;
using Microsoft.AspNet.SignalR;
using Microsoft.AspNet.SignalR.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace CobanaEnergy.Project.Service.BackgroundServices
{
    public class CampaignMonitorService
    {
        private readonly ApplicationDBContext _db;
        private readonly IHubContext _hubContext;

        public CampaignMonitorService(ApplicationDBContext db, IConnectionManager connectionManager)
        {
            _db = db;
            _hubContext = connectionManager.GetHubContext<NotificationHub.NotificationHub>();
        }
        #region [Check Campaigns]

        public async Task CheckAndSendCampaignNotificationsAsync()
         {
            var now = DateTime.Now;
            // Step 1: Get active campaigns
            var campaigns = await _db.CE_Campaigns
                .Where(c => c.StartDate <= now && c.EndDate >= now) // 04/08/2025 <= 05/08/2025 && 14/08/2025 >= 05/08/2025
                .ToListAsync();

            foreach (var campaign in campaigns)
            {
                var supplierId = campaign.SupplierId;
                var productId = campaign.ProductId;
                var startDate = campaign.StartDate.Date;
                var endDate = campaign.EndDate.Date;
                var saleTarget = int.TryParse(campaign.SaleTarget, out var targetVal) ? targetVal : 0;

                // Step 2: Fetch and filter gas contracts
                var gasContracts = (await _db.CE_GasContracts
                    .Where(gc => gc.SupplierId == supplierId && (!productId.HasValue || gc.ProductId == productId))
                    .ToListAsync())
                    .Where(gc =>
                        DateTime.TryParse(gc.InputDate, out var inputDate) &&
                        inputDate.Date >= startDate &&
                        inputDate.Date <= endDate
                    ).ToList();

                // Step 3: Fetch and filter electric contracts
                var electricContracts = (await _db.CE_ElectricContracts
                    .Where(ec => ec.SupplierId == supplierId && (!productId.HasValue || ec.ProductId == productId))
                    .ToListAsync())
                    .Where(ec =>
                        DateTime.TryParse(ec.InputDate, out var inputDate) &&
                        inputDate.Date >= startDate &&
                        inputDate.Date <= endDate
                    ).ToList();

                // Step 4: Count check
                var totalContracts = gasContracts.Count + electricContracts.Count;
                if (totalContracts >= saleTarget)
                {
                    // Step 5: Avoid duplicate notification
                    var existingNotification = await _db.CE_CampaignNotifications
                        .FirstOrDefaultAsync(n => n.CampaignId == campaign.Id);

                    if (existingNotification == null)
                    {
                        existingNotification = new CE_CampaignNotification
                        {
                            CampaignId = campaign.Id,
                            NotifiedAt = DateTime.Now,
                            Message = $"🎯 Sale target achieved for campaign: {campaign.CampaignName}. 🎉🌟💯🥳"
                        };
                        _db.CE_CampaignNotifications.Add(existingNotification);
                        await _db.SaveChangesAsync();
                    }
                    // Broadcast only to users who haven't seen it
                    var connectedUsers = ConnectedUserStore.Users; 

                    foreach (var userId in connectedUsers)
                    {
                        // Check if this user already saw this notification
                        bool hasSeen = await _db.CE_UserNotificationStatus
                            .AnyAsync(n => n.UserId == userId && n.CampaignId == campaign.Id);
                        if (!hasSeen)
                        {
                            // Send SignalR message to specific user
                            _hubContext.Clients.User(userId).receiveCampaignNotification(existingNotification.Message);
                            // Track that this user has seen this campaign notification
                            var userNotification = new CE_UserNotificationStatus
                            {
                                UserId = userId,
                                CampaignId = campaign.Id,
                                SeenAt = now
                            };
                            _db.CE_UserNotificationStatus.Add(userNotification);
                        }
                    }

                    await _db.SaveChangesAsync();

                }

            }
        }


        #endregion

    }
}