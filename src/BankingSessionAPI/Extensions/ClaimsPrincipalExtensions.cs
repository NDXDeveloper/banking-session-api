using System.Security.Claims;

namespace BankingSessionAPI.Extensions;

public static class ClaimsPrincipalExtensions
{
    public static string? GetUserId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
    }

    public static string? GetUserName(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Name)?.Value;
    }

    public static string? GetEmail(this ClaimsPrincipal principal)
    {
        return principal.FindFirst(ClaimTypes.Email)?.Value;
    }

    public static IEnumerable<string> GetRoles(this ClaimsPrincipal principal)
    {
        return principal.FindAll(ClaimTypes.Role).Select(c => c.Value);
    }

    public static bool IsInRole(this ClaimsPrincipal principal, string role)
    {
        return principal.FindAll(ClaimTypes.Role).Any(c => c.Value == role);
    }

    public static string? GetSessionId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("session_id")?.Value;
    }

    public static string? GetDeviceId(this ClaimsPrincipal principal)
    {
        return principal.FindFirst("device_id")?.Value;
    }
}