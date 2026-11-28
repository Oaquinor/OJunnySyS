namespace OJunnySyS.Application.Contracts;

public sealed record TenantUserDto(Guid Id, string DisplayName, string Email, string Role, bool IsActive, bool MustChangePassword, bool EmailConfirmed, DateTimeOffset? LockoutEnd);
public sealed record TenantMemberDto(Guid Id, string DisplayName);
public sealed record CreateTenantUserRequest(string DisplayName, string Email, string Role);
public sealed record TenantInvitationTokenDto(TenantUserDto User, string Token);
public sealed record TenantInvitationResult(TenantUserDto User, bool EmailSent, string? LocalInvitationUrl);
public sealed record SetTenantUserActiveRequest(bool IsActive);

public static class TenantUserRoles
{
    public const string Administrator = "Administrator";
    public const string Manager = "Manager";
    public const string Cashier = "Cashier";
    public const string PurchasingAgent = "PurchasingAgent";
    public const string Salesperson = "Salesperson";
    public static readonly IReadOnlyList<string> Assignable = [Administrator, Manager, Salesperson, Cashier, PurchasingAgent];
}
