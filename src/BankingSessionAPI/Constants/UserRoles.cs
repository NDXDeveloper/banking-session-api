namespace BankingSessionAPI.Constants;

public static class UserRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string User = "User";
    public const string ReadOnly = "ReadOnly";

    public static readonly string[] AllRoles = 
    {
        SuperAdmin,
        Admin,
        Manager,
        User,
        ReadOnly
    };

    public static readonly string[] AdminRoles = 
    {
        SuperAdmin,
        Admin
    };

    public static readonly string[] ManagerRoles = 
    {
        SuperAdmin,
        Admin,
        Manager
    };
}