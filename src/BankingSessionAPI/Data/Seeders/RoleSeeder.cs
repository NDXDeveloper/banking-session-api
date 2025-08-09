using Microsoft.AspNetCore.Identity;
using BankingSessionAPI.Constants;

namespace BankingSessionAPI.Data.Seeders;

public static class RoleSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        var roles = new[] 
        {
            UserRoles.SuperAdmin,
            UserRoles.Admin,
            UserRoles.Manager,
            UserRoles.User,
            UserRoles.ReadOnly
        };

        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var identityRole = new IdentityRole(role)
                {
                    NormalizedName = role.ToUpperInvariant()
                };
                
                var result = await roleManager.CreateAsync(identityRole);
                
                if (!result.Succeeded)
                {
                    throw new Exception($"Erreur lors de la création du rôle {role}: {string.Join(", ", result.Errors.Select(e => e.Description))}");
                }
            }
        }
    }
}