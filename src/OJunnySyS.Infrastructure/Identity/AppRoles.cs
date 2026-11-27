namespace OJunnySyS.Infrastructure.Identity;

public static class AppRoles
{
    public const string Administrator = "Administrator";
    public const string PlatformAdministrator = "PlatformAdministrator";
    public const string Salesperson = "Salesperson";
    public const string PurchasingAgent = "PurchasingAgent";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public static readonly string[] All = [PlatformAdministrator, Administrator, Manager, Salesperson, PurchasingAgent, Cashier];
}
