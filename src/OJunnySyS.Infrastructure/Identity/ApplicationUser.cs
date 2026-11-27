using Microsoft.AspNetCore.Identity;

namespace OJunnySyS.Infrastructure.Identity;

public sealed class ApplicationUser : IdentityUser<Guid>
{
    public string DisplayName { get; set; } = string.Empty;
    public Guid? TenantId { get; set; }
    public bool MustChangePassword { get; set; }
}
