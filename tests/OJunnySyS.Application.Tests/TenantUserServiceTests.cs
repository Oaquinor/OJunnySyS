using System.Net.Mail;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Application.Services;

namespace OJunnySyS.Application.Tests;

[TestClass]
public sealed class TenantUserServiceTests
{
    [TestMethod]
    public async Task Invite_WhenEmailIsNotConfiguredOutsideDevelopment_DoesNotCreatePendingUser()
    {
        var repository = new FakeTenantUserRepository();
        var email = new FakeAccountEmailSender { IsConfigured = false };
        var service = new TenantUserService(repository, email);

        await ExpectAsync<InvalidOperationException>(() => service.InviteAsync(
            ValidRequest, "https://tenant.example/", allowLocalLink: false));

        Assert.AreEqual(0, repository.CreateCalls);
        Assert.AreEqual(0, email.InvitationCalls);
    }

    [TestMethod]
    public async Task Invite_InDevelopment_ReturnsOneTimeLinkWithoutShowingTokenSeparately()
    {
        var repository = new FakeTenantUserRepository();
        var email = new FakeAccountEmailSender { IsConfigured = false };
        var service = new TenantUserService(repository, email);

        var result = await service.InviteAsync(ValidRequest, "http://localhost:5147/", allowLocalLink: true);

        Assert.IsFalse(result.EmailSent);
        Assert.IsNotNull(result.LocalInvitationUrl);
        Assert.AreEqual("http://localhost:5147/account/accept-invitation", new Uri(result.LocalInvitationUrl).GetLeftPart(UriPartial.Path));
        Assert.IsTrue(Uri.UnescapeDataString(new Uri(result.LocalInvitationUrl).Query).Contains("token=a+b/c==", StringComparison.Ordinal));
        Assert.AreEqual(0, email.InvitationCalls);
    }

    [TestMethod]
    public async Task Invite_WithSmtpConfigured_SendsInvitationAndDoesNotReturnSecretLink()
    {
        var repository = new FakeTenantUserRepository();
        var email = new FakeAccountEmailSender { IsConfigured = true };
        var service = new TenantUserService(repository, email);

        var result = await service.InviteAsync(ValidRequest, "https://tenant.example/", allowLocalLink: false);

        Assert.IsTrue(result.EmailSent);
        Assert.IsNull(result.LocalInvitationUrl);
        Assert.AreEqual("oliver@example.com", email.LastRecipient);
        Assert.IsTrue(email.LastInvitationUrl!.StartsWith("https://tenant.example/account/accept-invitation?", StringComparison.Ordinal));
        Assert.AreEqual("Persona", repository.LastRequest!.DisplayName);
    }

    [TestMethod]
    public async Task Invite_WhenSmtpDeliveryFails_LeavesInvitationPendingForRetry()
    {
        var repository = new FakeTenantUserRepository();
        var email = new FakeAccountEmailSender { IsConfigured = true, FailInvitations = true };
        var service = new TenantUserService(repository, email);

        var result = await service.InviteAsync(ValidRequest, "https://tenant.example", allowLocalLink: false);

        Assert.IsFalse(result.EmailSent);
        Assert.IsNull(result.LocalInvitationUrl);
        Assert.AreEqual(1, repository.CreateCalls);
    }

    [TestMethod]
    public async Task Invite_WhenRoleIsNotAssignable_DoesNotCreateUser()
    {
        var repository = new FakeTenantUserRepository();
        var email = new FakeAccountEmailSender { IsConfigured = true };
        var service = new TenantUserService(repository, email);

        await ExpectAsync<ArgumentException>(() => service.InviteAsync(
            ValidRequest with { Role = "PlatformAdministrator" }, "https://tenant.example", allowLocalLink: false));

        Assert.AreEqual(0, repository.CreateCalls);
    }

    private static readonly CreateTenantUserRequest ValidRequest = new(" Persona ", "oliver@example.com", TenantUserRoles.Cashier);

    private static async Task<TException> ExpectAsync<TException>(Func<Task> action) where TException : Exception
    {
        try { await action(); }
        catch (TException ex) { return ex; }
        Assert.Fail($"Se esperaba {typeof(TException).Name}.");
        return null!;
    }

    private sealed class FakeTenantUserRepository : ITenantUserRepository
    {
        public int CreateCalls { get; private set; }
        public CreateTenantUserRequest? LastRequest { get; private set; }
        private static readonly TenantUserDto User = new(Guid.NewGuid(), "Persona", "oliver@example.com", TenantUserRoles.Cashier,
            IsActive: false, MustChangePassword: false, EmailConfirmed: false, LockoutEnd: null);

        public Task<IReadOnlyList<TenantUserDto>> GetUsersAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TenantUserDto>>([]);
        public Task<IReadOnlyList<TenantMemberDto>> GetActiveMembersAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TenantMemberDto>>([]);
        public Task<IReadOnlyList<TenantMemberDto>> GetActiveSalespeopleAsync(CancellationToken cancellationToken = default) => Task.FromResult<IReadOnlyList<TenantMemberDto>>([]);
        public Task<TenantInvitationTokenDto> CreateInvitationAsync(CreateTenantUserRequest request, CancellationToken cancellationToken = default)
        {
            CreateCalls++;
            LastRequest = request;
            return Task.FromResult(new TenantInvitationTokenDto(User, "a+b/c=="));
        }
        public Task<TenantInvitationTokenDto> GetInvitationTokenAsync(Guid userId, CancellationToken cancellationToken = default) =>
            Task.FromResult(new TenantInvitationTokenDto(User, "a+b/c=="));
        public Task<TenantUserDto> SetActiveAsync(Guid userId, Guid actorId, bool isActive, CancellationToken cancellationToken = default) => Task.FromResult(User);
    }

    private sealed class FakeAccountEmailSender : IAccountEmailSender
    {
        public bool IsConfigured { get; init; }
        public bool FailInvitations { get; init; }
        public int InvitationCalls { get; private set; }
        public string? LastRecipient { get; private set; }
        public string? LastInvitationUrl { get; private set; }
        public Task SendConfirmationAsync(string email, string confirmationUrl, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendPasswordResetAsync(string email, string resetUrl, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task SendInvitationAsync(string email, string invitationUrl, CancellationToken cancellationToken = default)
        {
            InvitationCalls++;
            LastRecipient = email;
            LastInvitationUrl = invitationUrl;
            if (FailInvitations) throw new SmtpException("SMTP test failure");
            return Task.CompletedTask;
        }
    }
}
