using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Logic.LockManager
{
    /// <summary>
    /// Generic thread-safe lock manager for entities of type T.
    /// Manages locks using ConcurrentDictionary to ensure thread safety across multiple users.
    /// Uses string-based entity IDs with heartbeat functionality.
    /// </summary>
    /// <typeparam name="T">The entity type marker (e.g., Contracts, Accounts)</typeparam>
    public class EntityLockManager<T>
    {
        /// <summary>
        /// Information about a lock including heartbeat data.
        /// </summary>
        public class LockInfo
        {
            public string UserId { get; set; }
            public DateTime AcquiredAt { get; set; }
            public DateTime LastHeartbeat { get; set; }
            
            /// <summary>
            /// Check if lock is expired based on multiple conditions.
            /// </summary>
            public bool IsExpired => 
                DateTime.UtcNow > LastHeartbeat.AddMinutes(1) ||  // No heartbeat for 1 minute (2 missed intervals)
                DateTime.UtcNow > AcquiredAt.AddMinutes(60);      // Lock older than 1 hour (max age safety)
        }

        /// <summary>
        /// Thread-safe dictionary storing entity locks.
        /// Key: Entity ID, Value: User ID who holds the lock
        /// </summary>
        private readonly ConcurrentDictionary<string, string> _stringLocks;

        /// <summary>
        /// Thread-safe dictionary storing entity locks with heartbeat info.
        /// Key: Entity ID, Value: LockInfo with heartbeat data
        /// </summary>
        private readonly ConcurrentDictionary<string, LockInfo> _stringLocksWithHeartbeat;

        public EntityLockManager()
        {
            _stringLocks = new ConcurrentDictionary<string, string>();
            _stringLocksWithHeartbeat = new ConcurrentDictionary<string, LockInfo>();
        }

        /// <summary>
        /// Gets the total number of active locks for this entity type.
        /// </summary>
        /// <returns>The number of active locks</returns>
        public int GetActiveLockCount()
        {
            return _stringLocks.Count;
        }

        /// <summary>
        /// Attempts to acquire a lock on the specified entity for the given user.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to lock</param>
        /// <param name="userId">The user ID attempting to acquire the lock</param>
        /// <returns>
        /// True if the lock was successfully acquired or the user already holds the lock;
        /// False if another user already holds the lock
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when userId is null or empty</exception>
        public bool TryLock(string entityId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "UserId cannot be null or empty");
            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentNullException(nameof(entityId), "EntityId cannot be null or empty");

            // Clean expired locks first
            CleanupExpiredLocks();

            // Create new lock info
            var lockInfo = new LockInfo
            {
                UserId = userId,
                AcquiredAt = DateTime.UtcNow,
                LastHeartbeat = DateTime.UtcNow
            };

            // Try to add the lock with heartbeat info
            var addedLockInfo = _stringLocksWithHeartbeat.AddOrUpdate(
                entityId,
                lockInfo, // Value to add if key doesn't exist
                (key, existingLockInfo) => existingLockInfo.UserId == userId ? lockInfo : existingLockInfo // Keep existing if different user
            );

            // Also maintain backward compatibility with simple string locks
            var lockHolder = _stringLocks.AddOrUpdate(
                entityId,
                userId, // Value to add if key doesn't exist
                (key, existingUser) => existingUser == userId ? userId : existingUser // Keep existing if different user
            );

            // Return true if we got the lock or we already had it
            return addedLockInfo.UserId == userId;
        }

        /// <summary>
        /// Attempts to release a lock on the specified entity.
        /// Only the user who holds the lock can release it.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to unlock</param>
        /// <param name="userId">The user ID attempting to release the lock</param>
        /// <returns>
        /// True if the lock was successfully released or no lock existed;
        /// False if the lock is held by a different user
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown when userId is null or empty</exception>
        public bool Unlock(string entityId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "UserId cannot be null or empty");
            if (string.IsNullOrWhiteSpace(entityId))
                throw new ArgumentNullException(nameof(entityId), "EntityId cannot be null or empty");

            // Try to get the current lock holder from heartbeat locks
            if (_stringLocksWithHeartbeat.TryGetValue(entityId, out var lockInfo))
            {
                // Only allow the lock holder to unlock
                if (lockInfo.UserId != userId)
                {
                    return false;
                }

                // Remove from both dictionaries
                _stringLocksWithHeartbeat.TryRemove(entityId, out _);
                _stringLocks.TryRemove(entityId, out _);
                return true;
            }

            // Fallback to simple string locks for backward compatibility
            if (!_stringLocks.TryGetValue(entityId, out string currentHolder))
            {
                // No lock exists, consider this a successful unlock
                return true;
            }

            // Only allow the lock holder to unlock
            if (currentHolder != userId)
            {
                return false;
            }

            // Remove the lock
            return _stringLocks.TryRemove(entityId, out _);
        }

        /// <summary>
        /// Checks if the specified entity is currently locked.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check</param>
        /// <returns>True if the entity is locked; False otherwise</returns>
        public bool IsLocked(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return false;
            
            // Clean expired locks first
            CleanupExpiredLocks();
            
            return _stringLocksWithHeartbeat.ContainsKey(entityId) || _stringLocks.ContainsKey(entityId);
        }

        /// <summary>
        /// Gets the user who currently holds the lock on the specified entity.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check</param>
        /// <returns>The user who holds the lock, or null if no lock exists</returns>
        public string GetLockHolder(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return null;
            
            // Clean expired locks first
            CleanupExpiredLocks();
            
            // Check heartbeat locks first
            if (_stringLocksWithHeartbeat.TryGetValue(entityId, out var lockInfo))
                return lockInfo.UserId;
            
            // Fallback to simple locks
            _stringLocks.TryGetValue(entityId, out string lockHolder);
            return lockHolder;
        }

        /// <summary>
        /// Checks if the specified user holds the lock on the specified entity.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to check</param>
        /// <param name="userId">The user ID to check</param>
        /// <returns>True if the user holds the lock; False otherwise</returns>
        /// <exception cref="ArgumentNullException">Thrown when userId is null or empty</exception>
        public bool IsLockedByUser(string entityId, string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "UserId cannot be null or empty");
            if (string.IsNullOrWhiteSpace(entityId))
                return false;

            return GetLockHolder(entityId) == userId;
        }

        /// <summary>
        /// Forces the removal of a lock regardless of who holds it.
        /// This method should be used with caution, typically for administrative purposes.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to force unlock</param>
        /// <returns>True if a lock was removed; False if no lock existed</returns>
        public bool ForceUnlock(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return false;
            return _stringLocks.TryRemove(entityId, out _);
        }

        /// <summary>
        /// Removes all locks held by the specified user.
        /// Useful for cleanup when a user logs out or session expires.
        /// </summary>
        /// <param name="userId">The user ID whose locks should be removed</param>
        /// <returns>The number of locks that were removed</returns>
        /// <exception cref="ArgumentNullException">Thrown when userId is null or empty</exception>
        public int RemoveAllLocksForUser(string userId)
        {
            if (string.IsNullOrWhiteSpace(userId))
                throw new ArgumentNullException(nameof(userId), "UserId cannot be null or empty");

            int removedCount = 0;
            
            // Handle string-based locks with heartbeat
            var heartbeatKeysToRemove = new List<string>();
            foreach (var kvp in _stringLocksWithHeartbeat)
            {
                if (kvp.Value.UserId == userId)
                {
                    heartbeatKeysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in heartbeatKeysToRemove)
            {
                if (_stringLocksWithHeartbeat.TryRemove(key, out var removedLockInfo) && removedLockInfo.UserId == userId)
                {
                    // Also remove from simple locks
                    _stringLocks.TryRemove(key, out _);
                    removedCount++;
                }
            }

            // Handle remaining simple string-based locks
            var stringKeysToRemove = new List<string>();
            foreach (var kvp in _stringLocks)
            {
                if (kvp.Value == userId && !_stringLocksWithHeartbeat.ContainsKey(kvp.Key)) // Don't double count
                {
                    stringKeysToRemove.Add(kvp.Key);
                }
            }
            foreach (var key in stringKeysToRemove)
            {
                if (_stringLocks.TryRemove(key, out string removedUser) && removedUser == userId)
                {
                    removedCount++;
                }
            }

            return removedCount;
        }

        // ===== HEARTBEAT METHODS =====

        /// <summary>
        /// Updates the heartbeat timestamp for the specified entity lock.
        /// Only the lock holder can update the heartbeat.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity</param>
        /// <param name="userId">The user ID attempting to update the heartbeat</param>
        /// <returns>True if heartbeat was updated; False if lock not found or not owned by user</returns>
        public bool UpdateHeartbeat(string entityId, string userId)
        {
            if (string.IsNullOrWhiteSpace(entityId) || string.IsNullOrWhiteSpace(userId))
                return false;

            if (_stringLocksWithHeartbeat.TryGetValue(entityId, out var lockInfo))
            {
                if (lockInfo.UserId == userId && !lockInfo.IsExpired)
                {
                    lockInfo.LastHeartbeat = DateTime.UtcNow;
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Cleans up expired locks from the heartbeat dictionary.
        /// </summary>
        /// <returns>The number of expired locks that were removed</returns>
        public int CleanupExpiredLocks()
        {
            var expiredKeys = _stringLocksWithHeartbeat
                .Where(kvp => kvp.Value.IsExpired)
                .Select(kvp => kvp.Key)
                .ToList();

            int removedCount = 0;
            foreach (var key in expiredKeys)
            {
                if (_stringLocksWithHeartbeat.TryRemove(key, out _))
                {
                    // Also remove from simple locks for consistency
                    _stringLocks.TryRemove(key, out _);
                    removedCount++;
                }
            }

            return removedCount;
        }

        /// <summary>
        /// Gets all active locks with their information.
        /// </summary>
        /// <returns>Dictionary of active locks with lock information</returns>
        public Dictionary<string, LockInfo> GetAllActiveLocks()
        {
            CleanupExpiredLocks();
            return new Dictionary<string, LockInfo>(_stringLocksWithHeartbeat);
        }

        /// <summary>
        /// Forces the removal of a lock regardless of who holds it (heartbeat version).
        /// This method should be used with caution, typically for administrative purposes.
        /// </summary>
        /// <param name="entityId">The unique identifier of the entity to force unlock</param>
        /// <returns>True if a lock was removed; False if no lock existed</returns>
        public bool ForceUnlockHeartbeat(string entityId)
        {
            if (string.IsNullOrWhiteSpace(entityId))
                return false;
            
            bool removedHeartbeat = _stringLocksWithHeartbeat.TryRemove(entityId, out _);
            bool removedSimple = _stringLocks.TryRemove(entityId, out _);
            
            return removedHeartbeat || removedSimple;
        }
    }
}
