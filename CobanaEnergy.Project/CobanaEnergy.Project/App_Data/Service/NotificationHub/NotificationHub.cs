using CobanaEnergy.Project.Service.UserService;
using Logic.LockManager;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.SignalR;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web;

namespace CobanaEnergy.Project.Service.NotificationHub
{
    public class NotificationHub : Hub
    {
        public override Task OnConnected()
        {
            string userId = Context.User.Identity.GetUserId();
            string connectionId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(userId))
            {
                bool wasOffline = !ConnectedUserStore.IsUserConnected(userId);
                ConnectedUserStore.AddUser(userId, connectionId);
                if (wasOffline)
                    BroadcastUserStatus(userId, "Online");
            }
            return base.OnConnected();
        }

        public override Task OnDisconnected(bool stopCalled)
        {
            string userId = Context.User.Identity.GetUserId();
            string connectionId = Context.ConnectionId;

            if (!string.IsNullOrEmpty(userId))
            {
                ConnectedUserStore.RemoveUser(userId, connectionId);
                
                // Clean up locks when user goes completely offline
                if (!ConnectedUserStore.IsUserConnected(userId))
                {
                    CleanupUserLocks(userId);
                    BroadcastUserStatus(userId, "Offline");
                }
            }
            return base.OnDisconnected(stopCalled);
        }

        public static void ForceLogout(string userId, string message)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            var connectionIds = ConnectedUserStore.GetUserConnectionIds(userId);
            foreach (var connectionId in connectionIds)
            {
                context.Clients.Client(connectionId).forceLogout(message);
            }
            
            // Clean up locks for forced logout
            CleanupUserLocks(userId);
        }

        public void NotifyLogout()
        {
            string userId = Context.User.Identity.GetUserId();
            string connId = Context.ConnectionId;
            if (!string.IsNullOrEmpty(userId))
            {
                ConnectedUserStore.RemoveUser(userId, connId);
                
                // Clean up locks when user explicitly logs out
                if (!ConnectedUserStore.IsUserConnected(userId))
                {
                    CleanupUserLocks(userId);
                    BroadcastUserStatus(userId, "Offline");
                }
            }
        }

        public static void BroadcastUserStatus(string userId, string status)
        {
            var context = GlobalHost.ConnectionManager.GetHubContext<NotificationHub>();
            context.Clients.All.updateUserStatus(userId, status);
        }

        
        // Cleans up all locks held by the specified user.This ensures contracts are not left locked when users disconnect.
        private static void CleanupUserLocks(string userId)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(userId))
                {
                    int removedLocks = LockManager.RemoveAllLocksForUser(userId);
                    if (removedLocks > 0)
                    {
                        Logic.Logger.Log($"NotificationHub: Cleaned up {removedLocks} locks for user {userId} on disconnect");
                    }
                }
            }
            catch (Exception ex)
            {
                // Log but don't throw - don't break SignalR flow
                Logic.Logger.Log($"NotificationHub: Error cleaning up locks for user {userId}: {ex.Message}");
            }
        }

    }

}