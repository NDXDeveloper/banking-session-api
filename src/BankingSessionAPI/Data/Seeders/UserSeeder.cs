using BankingSessionAPI.Constants;
using BankingSessionAPI.Models.Entities;
using Microsoft.AspNetCore.Identity;

namespace BankingSessionAPI.Data.Seeders;

public static class UserSeeder
{
    public static async Task SeedUsersAsync(UserManager<ApplicationUser> userManager)
    {
        // Créer l'utilisateur Super Admin par défaut
        var superAdminEmail = "superadmin@banking-api.com";
        
        if (await userManager.FindByEmailAsync(superAdminEmail) == null)
        {
            var superAdmin = new ApplicationUser
            {
                UserName = superAdminEmail,
                Email = superAdminEmail,
                EmailConfirmed = true,
                FirstName = "Super",
                LastName = "Admin",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PhoneNumberConfirmed = true
            };

            var superAdminPassword = Environment.GetEnvironmentVariable("DEFAULT_SUPER_ADMIN_PASSWORD") ?? "ChangeMe123!";
            var result = await userManager.CreateAsync(superAdmin, superAdminPassword);

            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(superAdmin, UserRoles.SuperAdmin);
            }
            else
            {
                throw new Exception($"Erreur lors de la création du Super Admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Créer un utilisateur Admin par défaut
        var adminEmail = "admin@banking-api.com";
        
        if (await userManager.FindByEmailAsync(adminEmail) == null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                EmailConfirmed = true,
                FirstName = "Admin",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PhoneNumberConfirmed = true
            };

            var adminPassword = Environment.GetEnvironmentVariable("DEFAULT_ADMIN_PASSWORD") ?? "ChangeMe123!";
            var result = await userManager.CreateAsync(admin, adminPassword);
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(admin, UserRoles.Admin);
            }
            else
            {
                throw new Exception($"Erreur lors de la création de l'Admin: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }

        // Créer un utilisateur de test
        var testUserEmail = "testuser@banking-api.com";
        
        if (await userManager.FindByEmailAsync(testUserEmail) == null)
        {
            var testUser = new ApplicationUser
            {
                UserName = testUserEmail,
                Email = testUserEmail,
                EmailConfirmed = true,
                FirstName = "Test",
                LastName = "User",
                IsActive = true,
                CreatedAt = DateTime.UtcNow,
                PhoneNumber = "+33123456789",
                PhoneNumberConfirmed = true
            };

            var testUserPassword = Environment.GetEnvironmentVariable("DEFAULT_TEST_USER_PASSWORD") ?? "ChangeMe123!";
            var result = await userManager.CreateAsync(testUser, testUserPassword);
            
            if (result.Succeeded)
            {
                await userManager.AddToRoleAsync(testUser, UserRoles.User);
            }
            else
            {
                throw new Exception($"Erreur lors de la création de l'utilisateur de test: {string.Join(", ", result.Errors.Select(e => e.Description))}");
            }
        }
    }
}