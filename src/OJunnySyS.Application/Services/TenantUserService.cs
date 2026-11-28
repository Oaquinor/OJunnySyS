using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;

namespace OJunnySyS.Application.Services;

public sealed class TenantUserService(ITenantUserRepository repository, IAccountEmailSender emailSender)
{
    public Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(CancellationToken ct = default) => repository.GetUsersAsync(ct);
    public Task<IReadOnlyList<TenantMemberDto>> GetActiveMembersAsync(CancellationToken ct = default) => repository.GetActiveMembersAsync(ct);
    public Task<IReadOnlyList<TenantMemberDto>> GetActiveSalespeopleAsync(CancellationToken ct = default) => repository.GetActiveSalespeopleAsync(ct);
    public async Task<TenantInvitationResult> InviteAsync(CreateTenantUserRequest request, string baseUri, bool allowLocalLink, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Trim().Length > 160)
            throw new ArgumentException("El nombre es obligatorio y admite hasta 160 caracteres.");
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Trim().Length > 254 || !request.Email.Contains('@'))
            throw new ArgumentException("Escribe un correo electrónico válido.");
        if (!TenantUserRoles.Assignable.Contains(request.Role, StringComparer.Ordinal))
            throw new ArgumentException("El rol seleccionado no se puede asignar.");
        if (!emailSender.IsConfigured && !allowLocalLink)
            throw new InvalidOperationException("Configura el correo transaccional antes de invitar a usuarios.");

        var invitation = await repository.CreateInvitationAsync(
            request with { DisplayName = request.DisplayName.Trim(), Email = request.Email.Trim(), Role = request.Role.Trim() }, ct);
        return await DeliverInvitationAsync(invitation, baseUri, allowLocalLink, ct);
    }

    public async Task<TenantInvitationResult> ResendInvitationAsync(Guid userId, string baseUri, bool allowLocalLink, CancellationToken ct = default)
    {
        if (!emailSender.IsConfigured && !allowLocalLink)
            throw new InvalidOperationException("Configura el correo transaccional antes de reenviar invitaciones.");
        var invitation = await repository.GetInvitationTokenAsync(userId, ct);
        return await DeliverInvitationAsync(invitation, baseUri, allowLocalLink, ct);
    }

    private async Task<TenantInvitationResult> DeliverInvitationAsync(TenantInvitationTokenDto invitation, string baseUri, bool allowLocalLink, CancellationToken ct)
    {
        var link = BuildInvitationUrl(baseUri, invitation.User.Id, invitation.Token);
        if (!emailSender.IsConfigured)
            return new TenantInvitationResult(invitation.User, false, allowLocalLink ? link : null);
        try
        {
            await emailSender.SendInvitationAsync(invitation.User.Email, link, ct);
            return new TenantInvitationResult(invitation.User, true, null);
        }
        catch (Exception ex) when (ex is InvalidOperationException or System.Net.Mail.SmtpException or IOException)
        {
            return new TenantInvitationResult(invitation.User, false, null);
        }
    }

    private static string BuildInvitationUrl(string baseUri, Guid userId, string token)
    {
        if (!Uri.TryCreate(baseUri, UriKind.Absolute, out var origin) || origin.Scheme is not ("http" or "https"))
            throw new ArgumentException("La dirección del sitio no es válida.", nameof(baseUri));
        var encoded = System.Net.WebUtility.UrlEncode(token);
        return $"{origin.GetLeftPart(UriPartial.Authority)}/account/accept-invitation?userId={userId:D}&token={encoded}";
    }

    public Task<TenantUserDto> SetActiveAsync(Guid userId, Guid actorId, bool isActive, CancellationToken ct = default) => repository.SetActiveAsync(userId, actorId, isActive, ct);
}
