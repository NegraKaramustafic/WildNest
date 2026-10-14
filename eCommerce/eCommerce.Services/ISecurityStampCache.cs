namespace eCommerce.Services;

/// <summary>
/// Caches the current user security stamp so JWT validation does not query the database on every call.
/// Logout, password change, and deactivation must invalidate the cached value.
/// </summary>
public interface ISecurityStampCache
{
    Task<string?> GetOrLoadAsync(int userId, Func<Task<string?>> load);

    void Invalidate(int userId);
}
