namespace Logic.LockManager
{
    /// <summary>
    /// Static lock manager providing strongly-typed access to entity lock managers.
    /// This class maintains application-wide lock state for all entity types.
    /// Thread-safe and designed for concurrent access by multiple users.
    /// </summary>
    public static class LockManager
    {
        
        public static readonly EntityLockManager<Contracts> Contracts = new EntityLockManager<Contracts>();
        //public static readonly EntityLockManager<Accounts> Accounts = new EntityLockManager<Accounts>(); Can be added later

        public static int RemoveAllLocksForUser(string user)
        {
            if (string.IsNullOrWhiteSpace(user))
                throw new System.ArgumentNullException(nameof(user), "User cannot be null or empty");

            int totalRemoved = 0;
            totalRemoved += Contracts.RemoveAllLocksForUser(user);
            return totalRemoved;
        }

        /// <summary>
        /// Cleans up expired locks across all entity types.
        /// </summary>
        /// <returns>Total number of expired locks removed</returns>
        public static int CleanupAllExpiredLocks()
        {
            int totalRemoved = 0;
            totalRemoved += Contracts.CleanupExpiredLocks();
            // totalRemoved += Accounts.CleanupExpiredLocks(); // Can be added later
            return totalRemoved;
        }
    }
}
