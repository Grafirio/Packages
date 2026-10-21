namespace Grafirio.Shared.Identity.Permissions;

/// <summary>An authority failure is not an authorization denial.</summary>
public sealed class PermissionUnavailableException(string message, Exception? inner = null)
    : Exception(message, inner);

public static class PlatformPermissions
{
    public const string Read = "PLATFORM.READ";
    public const string Manage = "PLATFORM.MANAGE";
    public static readonly string[] All = [Read, Manage];
}
