using BankingSessionAPI.Models.DTOs;
using BankingSessionAPI.Models.Entities;
using BankingSessionAPI.Models.Requests;

namespace BankingSessionAPI.Services.Interfaces;

public interface IAccountService
{
    Task<UserDto?> GetUserByIdAsync(string userId);
    Task<UserDto?> GetUserByEmailAsync(string email);
    Task<UserDto?> GetUserByUsernameAsync(string username);
    Task<ApplicationUser?> CreateUserAsync(CreateAccountRequest request);
    Task<bool> UpdateUserAsync(string userId, UserDto userDto);
    Task<bool> DeactivateUserAsync(string userId, string deactivatedBy, string reason);
    Task<bool> ActivateUserAsync(string userId, string activatedBy);
    Task<bool> ChangePasswordAsync(string userId, ChangePasswordRequest request);
    Task<bool> ResetPasswordAsync(string email);
    Task<bool> ConfirmEmailAsync(string userId, string token);
    Task<bool> UpdateLastLoginAsync(string userId);
    Task<IEnumerable<UserDto>> GetUsersAsync(int skip = 0, int take = 50, string? search = null, 
        bool? isActive = null, string? role = null);
    Task<int> GetUsersCountAsync(string? search = null, bool? isActive = null, string? role = null);
    Task<IEnumerable<string>> GetUserRolesAsync(string userId);
    Task<bool> AddUserToRoleAsync(string userId, string role);
    Task<bool> RemoveUserFromRoleAsync(string userId, string role);
    Task<bool> IsInRoleAsync(string userId, string role);
    Task<bool> UserExistsAsync(string userId);
    Task<bool> EmailExistsAsync(string email);
    Task<bool> UsernameExistsAsync(string username);
    Task<Dictionary<string, object>> GetUserStatisticsAsync();
    Task<IEnumerable<UserDto>> GetRecentlyActiveUsersAsync(int count = 10);
    Task<IEnumerable<UserDto>> GetLockedUsersAsync();
    Task<bool> RequiresPasswordChangeAsync(string userId);
    Task<bool> SetPasswordChangeRequiredAsync(string userId, bool required);
    Task<DateTime?> GetLastPasswordChangeAsync(string userId);
    Task<bool> ValidateUserCredentialsAsync(string email, string password);
    Task<ApplicationUser?> GetApplicationUserByIdAsync(string userId);
    Task<ApplicationUser?> GetApplicationUserByEmailAsync(string email);
}