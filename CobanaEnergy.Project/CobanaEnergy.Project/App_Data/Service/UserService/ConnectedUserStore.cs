using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace CobanaEnergy.Project.Service.UserService
{
    public static class ConnectedUserStore
    {
        private static readonly Dictionary<string, HashSet<string>> _userConnections = new Dictionary<string, HashSet<string>>();
        private static readonly object _lock = new object();

        public static void AddUser(string userId, string connectionId)
        {
            lock (_lock)
            {
                if (!_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId] = new HashSet<string>();
                }
                _userConnections[userId].Add(connectionId);
            }
        }

        public static void RemoveUser(string userId, string connectionId)
        {
            lock (_lock)
            {
                if (_userConnections.ContainsKey(userId))
                {
                    _userConnections[userId].Remove(connectionId);
                    if (_userConnections[userId].Count == 0)
                    {
                        _userConnections.Remove(userId);
                    }
                }
            }
        }

        public static List<string> Users
        {
            get
            {
                lock (_lock)
                {
                    return _userConnections.Keys.ToList();
                }
            }
        }

        public static bool IsUserConnected(string userId)
        {
            lock (_lock)
            {
                return _userConnections.ContainsKey(userId);
            }
        }

        public static IEnumerable<string> GetUserConnectionIds(string userId)
        {
            lock (_lock)
            {
                if (_userConnections.ContainsKey(userId))
                {
                    return _userConnections[userId].ToList();
                }
                return Enumerable.Empty<string>();
            }
        }
    }

}