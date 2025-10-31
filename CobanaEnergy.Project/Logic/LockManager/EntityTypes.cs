namespace Logic.LockManager
{
    /// <summary>
    /// Marker class for Contract entities.
    /// Used as a type parameter for EntityLockManager to provide strong typing.
    /// </summary>
    public sealed class Contracts
    {
        // This class is intentionally empty and serves only as a type marker
        // to ensure type safety in the generic EntityLockManager<T>
        private Contracts() { }
    }

    /// <summary>
    /// Marker class for Account entities.
    /// Used as a type parameter for EntityLockManager to provide strong typing.
    /// </summary>
    public sealed class Accounts
    {
        // This class is intentionally empty and serves only as a type marker
        // to ensure type safety in the generic EntityLockManager<T>
        private Accounts() { }
    }
}
