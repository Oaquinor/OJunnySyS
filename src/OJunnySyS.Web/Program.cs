using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using System.Security.Claims;
using System.Threading.RateLimiting;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Application.Services;
using OJunnySyS.Domain.Common;
using OJunnySyS.Infrastructure;
using OJunnySyS.Infrastructure.Identity;
using OJunnySyS.Infrastructure.Persistence;
using OJunnySyS.Web.Identity;
using OJunnySyS.Web.Components;
using OJunnySyS.Web.Presentation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents().AddInteractiveServerComponents().AddInteractiveWebAssemblyComponents().AddAuthenticationStateSerialization();
builder.Services.AddCascadingAuthenticationState();
builder.Services.AddControllersWithViews();
builder.Services.AddRateLimiter(options =>
{
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.AddPolicy("registration", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 5,
            Window = TimeSpan.FromMinutes(15),
            QueueLimit = 0,
            AutoReplenishment = true
        }));
});
builder.Services.AddIdentity<ApplicationUser, IdentityRole<Guid>>(options =>
{
    options.User.RequireUniqueEmail = true;
    options.SignIn.RequireConfirmedAccount = true;
    options.Password.RequiredLength = 12;
    options.Password.RequireDigit = true;
    options.Password.RequireUppercase = true;
    options.Password.RequireLowercase = true;
    options.Password.RequireNonAlphanumeric = true;
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
})
    .AddEntityFrameworkStores<StoreDbContext>()
    .AddDefaultTokenProviders();
builder.Services.AddScoped<IUserClaimsPrincipalFactory<ApplicationUser>, TenantClaimsPrincipalFactory>();
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<TenantContext>();
builder.Services.AddScoped<OJunnySyS.Application.Abstractions.ITenantContext>(sp => sp.GetRequiredService<TenantContext>());
builder.Services.ConfigureApplicationCookie(options =>
{
    var validatePrincipal = options.Events.OnValidatePrincipal;
    options.LoginPath = "/account/login";
    options.AccessDeniedPath = "/account/denied";
    options.Cookie.Name = "OJunnySyS.Auth";
    options.Cookie.HttpOnly = true;
    options.Cookie.SameSite = SameSiteMode.Lax;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment() ? CookieSecurePolicy.SameAsRequest : CookieSecurePolicy.Always;
    options.SlidingExpiration = true;
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.Events.OnRedirectToLogin = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) { context.Response.StatusCode = StatusCodes.Status401Unauthorized; return Task.CompletedTask; }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnRedirectToAccessDenied = context =>
    {
        if (context.Request.Path.StartsWithSegments("/api")) { context.Response.StatusCode = StatusCodes.Status403Forbidden; return Task.CompletedTask; }
        context.Response.Redirect(context.RedirectUri);
        return Task.CompletedTask;
    };
    options.Events.OnValidatePrincipal = async context =>
    {
        if (validatePrincipal is not null) await validatePrincipal(context);
        if (context.Principal?.Identity?.IsAuthenticated != true || context.Principal.HasClaim(claim => claim.Type == "tenant_id")) return;
        var userManager = context.HttpContext.RequestServices.GetRequiredService<UserManager<ApplicationUser>>();
        var user = await userManager.GetUserAsync(context.Principal);
        if (user?.TenantId is null) return;
        var claimsFactory = context.HttpContext.RequestServices.GetRequiredService<IUserClaimsPrincipalFactory<ApplicationUser>>();
        context.ReplacePrincipal(await claimsFactory.CreateAsync(user));
        context.ShouldRenew = true;
    };
});
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("CatalogWrite", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager));
    options.AddPolicy("CrmWrite", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.Salesperson));
    options.AddPolicy("ReportsRead", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager));
    options.AddPolicy("SalesRead", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.Cashier));
    options.AddPolicy("SalesQuoteManage", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.Salesperson));
    options.AddPolicy("ReceivablesRead", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.Cashier));
    options.AddPolicy("CustomerStatementRead", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.Cashier, AppRoles.Salesperson));
    options.AddPolicy("Checkout", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.Cashier));
    options.AddPolicy("SalesReturn", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.Cashier));
    options.AddPolicy("PurchasingWrite", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.PurchasingAgent));
    options.AddPolicy("SupplierPayments", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager));
    options.AddPolicy("CashManagement", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager, AppRoles.Cashier));
    options.AddPolicy("TenantUserManagement", policy => policy.RequireRole(AppRoles.Administrator));
    options.AddPolicy("CrmManage", policy => policy.RequireRole(AppRoles.Administrator, AppRoles.Manager));
});
builder.Services.AddProblemDetails();

var connectionString = builder.Configuration.GetConnectionString("OjunnySyS")
    ?? throw new InvalidOperationException("Configure la conexión MySQL con ConnectionStrings__OjunnySyS.");
var businessTimeZone = builder.Configuration["BusinessTimeZone"] ?? "America/Santo_Domingo";
builder.Services.AddInfrastructure(connectionString, businessTimeZone);
builder.Services.AddScoped<StoreService>();
builder.Services.AddScoped<ParkedSaleService>();
builder.Services.AddScoped<SalesQuoteService>();
builder.Services.AddScoped<CrmService>();
builder.Services.AddScoped<PlatformManagementService>();
builder.Services.AddScoped<TenantSettingsService>();
builder.Services.AddScoped<TenantWorkspaceService>();
builder.Services.AddScoped<TenantUserService>();
builder.Services.AddScoped<CustomerTagService>();
builder.Services.AddScoped<CustomerSegmentService>();
builder.Services.AddScoped<PosRegisterService>();
builder.Services.AddScoped<WarehouseService>();
builder.Services.AddScoped<PosPaymentMethodService>();
builder.Services.AddScoped<TaxProfileService>();
builder.Services.AddScoped<AccountingService>();
builder.Services.AddScoped<BankingService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler();
    app.UseHsts();
}
app.UseWhen(context => !context.Request.Path.StartsWithSegments("/api"),
    branch => branch.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true));
app.UseHttpsRedirection();
app.UseRouting();
app.UseRateLimiter();
app.UseAuthentication();
app.UseAuthorization();
app.Use(async (context, next) =>
{
    context.RequestServices.GetRequiredService<TenantContext>().SetFromPrincipal(context.User);
    await next();
});
app.UseMiddleware<TenantAccessMiddleware>();
app.UseAntiforgery();

app.MapGet("/account/denied", () => Results.Content("<h1>Acceso denegado</h1><p>Tu cuenta no tiene permiso para esta sección.</p><a href='/'>Volver</a>", "text/html"));
app.MapControllers();

var paymentMethods = app.MapGroup("/api/v1/pos/payment-methods").RequireAuthorization("TenantUserManagement");
paymentMethods.MapGet("/", async (bool? includeInactive, PosPaymentMethodService service, CancellationToken ct) => Results.Ok(await service.GetAsync(includeInactive ?? false, ct)));
paymentMethods.MapPost("/", async (CreatePaymentMethodRequest request, PosPaymentMethodService service, CancellationToken ct) =>
{
    try { var method = await service.CreateAsync(request, ct); return Results.Created($"/api/v1/pos/payment-methods/{method.Id}", method); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["paymentMethod"] = [ex.Message] }); }
});
paymentMethods.MapPut("/{id:guid}", async (Guid id, UpdatePaymentMethodRequest request, PosPaymentMethodService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.UpdateAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["paymentMethod"] = [ex.Message] }); }
});
paymentMethods.MapPatch("/{id:guid}/activation", async (Guid id, SetPaymentMethodActiveRequest request, PosPaymentMethodService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.SetActiveAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["paymentMethod"] = [ex.Message] }); }
});
var taxProfiles = app.MapGroup("/api/v1/tax-profiles").RequireAuthorization("CatalogWrite");
taxProfiles.MapGet("/", async (bool? includeInactive, TaxProfileService service, CancellationToken ct) => Results.Ok(await service.GetAsync(includeInactive ?? false, ct)));
taxProfiles.MapPost("/", async (CreateTaxProfileRequest request, TaxProfileService service, CancellationToken ct) =>
{
    try { var profile = await service.CreateAsync(request, ct); return Results.Created($"/api/v1/tax-profiles/{profile.Id}", profile); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["taxProfile"] = [ex.Message] }); }
}).RequireAuthorization();
taxProfiles.MapPut("/{id:guid}", async (Guid id, UpdateTaxProfileRequest request, TaxProfileService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.UpdateAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["taxProfile"] = [ex.Message] }); }
});
taxProfiles.MapPatch("/{id:guid}/activation", async (Guid id, SetTaxProfileActiveRequest request, TaxProfileService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.SetActiveAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["taxProfile"] = [ex.Message] }); }
});
var api = app.MapGroup("/api/v1").RequireAuthorization();
var posClientApi = api.MapGroup("/pos/client").RequireAuthorization("Checkout");
posClientApi.MapGet("/terminals", async (PosRegisterService service, CancellationToken ct) =>
    Results.Ok(await service.GetRegistersAsync(includeInactive: false, ct)));
posClientApi.MapGet("/payment-options", async (PosPaymentMethodService service, CancellationToken ct) =>
    Results.Ok(await service.GetAsync(includeInactive: false, ct)));
posClientApi.MapGet("/sellers", async (TenantUserService service, CancellationToken ct) =>
    Results.Ok(await service.GetActiveSalespeopleAsync(ct)));
posClientApi.MapGet("/business-settings", async (TenantSettingsService service, CancellationToken ct) =>
    Results.Ok(await service.GetAsync(ct)));
posClientApi.MapGet("/warehouses/{warehouseId:guid}/products", async (Guid warehouseId, string? search, StoreService service, CancellationToken ct) =>
    Results.Ok(await service.SearchProductsInWarehouseAsync(search, warehouseId, ct)));
var tenantUsers = api.MapGroup("/users").RequireAuthorization("TenantUserManagement");
tenantUsers.MapGet("", async (TenantUserService service, CancellationToken ct) => Results.Ok(await service.GetUsersAsync(ct)));
tenantUsers.MapPost("", async (CreateTenantUserRequest request, HttpRequest httpRequest, TenantUserService service, CancellationToken ct) =>
{
    try { return Results.Created("/api/v1/users", await service.InviteAsync(request, $"{httpRequest.Scheme}://{httpRequest.Host}", allowLocalLink: false, ct: ct)); }
    catch (ArgumentException ex) { return Results.BadRequest(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
});
tenantUsers.MapPost("/{id:guid}/activation", async (Guid id, SetTenantUserActiveRequest request, ClaimsPrincipal principal, TenantUserService service, CancellationToken ct) =>
{
    if (!Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var actorId)) return Results.Unauthorized();
    try { return Results.Ok(await service.SetActiveAsync(id, actorId, request.IsActive, ct)); }
    catch (KeyNotFoundException ex) { return Results.NotFound(new { error = ex.Message }); }
    catch (InvalidOperationException ex) { return Results.Conflict(new { error = ex.Message }); }
});
var posRegisters = api.MapGroup("/pos/registers").RequireAuthorization("TenantUserManagement");
posRegisters.MapGet("", async (bool? includeInactive, PosRegisterService service, CancellationToken ct) =>
    Results.Ok(await service.GetRegistersAsync(includeInactive ?? true, ct)));
posRegisters.MapPost("", async (CreatePosRegisterRequest request, PosRegisterService service, CancellationToken ct) =>
{
    try { var register = await service.CreateAsync(request, ct); return Results.Created($"/api/v1/pos/registers/{register.Id}", register); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["register"] = [ex.Message] }); }
});
posRegisters.MapPut("/{id:guid}", async (Guid id, UpdatePosRegisterRequest request, PosRegisterService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.UpdateAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["register"] = [ex.Message] }); }
});
posRegisters.MapPatch("/{id:guid}/activation", async (Guid id, SetPosRegisterActiveRequest request, PosRegisterService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.SetActiveAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["register"] = [ex.Message] }); }
});
var warehouses = api.MapGroup("/inventory/warehouses").RequireAuthorization("TenantUserManagement");
warehouses.MapGet("", async (bool? includeInactive, WarehouseService service, CancellationToken ct) =>
    Results.Ok(await service.GetAsync(includeInactive ?? true, ct)));
warehouses.MapPost("", async (CreateWarehouseRequest request, WarehouseService service, CancellationToken ct) =>
{
    try { var warehouse = await service.CreateAsync(request, ct); return Results.Created($"/api/v1/inventory/warehouses/{warehouse.Id}", warehouse); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["warehouse"] = [ex.Message] }); }
});
warehouses.MapPut("/{id:guid}", async (Guid id, UpdateWarehouseRequest request, WarehouseService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.UpdateAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["warehouse"] = [ex.Message] }); }
});
warehouses.MapPatch("/{id:guid}/activation", async (Guid id, SetWarehouseActiveRequest request, WarehouseService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.SetActiveAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["warehouse"] = [ex.Message] }); }
});
var inventoryAdjustments = api.MapGroup("/inventory/adjustments").RequireAuthorization("CatalogWrite");
inventoryAdjustments.MapGet("", async (int? count, StoreService service, CancellationToken ct) =>
    Results.Ok(await service.GetInventoryAdjustmentsAsync(count ?? 100, ct)));
inventoryAdjustments.MapPost("", async (InventoryAdjustmentRequest request, ClaimsPrincipal user, StoreService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.AdjustInventoryAsync(request, CurrentActorId(user), ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["adjustment"] = [ex.Message] }); }
});
api.MapPost("/inventory/transfers", async (InventoryTransferRequest request, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.TransferInventoryAsync(request, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["inventory"] = [ex.Message] }); }
}).RequireAuthorization("CatalogWrite");
api.MapGet("/products", async (string? search, bool? includeInactive, ClaimsPrincipal principal, StoreService store, CancellationToken ct) =>
{
    var canSeeInactive = principal.IsInRole(AppRoles.Administrator) || principal.IsInRole(AppRoles.Manager);
    return Results.Ok(await store.SearchProductsAsync(search, includeInactive == true && canSeeInactive, ct));
});
api.MapGet("/customers", async (string? search, StoreService store, CancellationToken ct) => Results.Ok(await store.SearchCustomersAsync(search, ct)));
api.MapGet("/customers/{id:guid}", async (Guid id, StoreService store, CancellationToken ct) =>
{
    var customer = await store.GetCustomerAsync(id, ct);
    return customer is null ? Results.NotFound() : Results.Ok(customer);
}).RequireAuthorization("CrmWrite");
api.MapGet("/customers/{id:guid}/tags", async (Guid id, CustomerTagService tags, CancellationToken ct) => Results.Ok(await tags.GetCustomerTagsAsync(id, ct))).RequireAuthorization("CrmWrite");
api.MapPost("/customers/{id:guid}/tags", async (Guid id, AddCustomerTagRequest request, ClaimsPrincipal principal, CustomerTagService tags, CancellationToken ct) =>
{
    try { return Results.Ok(await tags.AddToCustomerAsync(id, request, CurrentActorId(principal), ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["tag"] = [ex.Message] }); }
}).RequireAuthorization("CrmManage");
api.MapDelete("/customers/{id:guid}/tags/{tagId:guid}", async (Guid id, Guid tagId, CustomerTagService tags, CancellationToken ct) =>
{
    await tags.RemoveFromCustomerAsync(id, tagId, ct);
    return Results.NoContent();
}).RequireAuthorization("CrmManage");
api.MapGet("/crm/tags", async (CustomerTagService tags, CancellationToken ct) => Results.Ok(await tags.GetTagsAsync(ct))).RequireAuthorization("CrmWrite");
var segments = api.MapGroup("/crm/segments").RequireAuthorization("CrmManage");
segments.MapGet("/", async (CustomerSegmentService service, CancellationToken ct) => Results.Ok(await service.GetSegmentsAsync(ct)));
segments.MapGet("/{id:guid}/customers", async (Guid id, int? limit, CustomerSegmentService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.GetMembersAsync(id, limit ?? 200, ct)); }
    catch (DomainException ex) { return Results.NotFound(new { error = ex.Message }); }
});
segments.MapPost("/", async (CreateCustomerSegmentRequest request, CustomerSegmentService service, CancellationToken ct) =>
{
    try { var segment = await service.CreateAsync(request, ct); return Results.Created($"/api/v1/crm/segments/{segment.Id}", segment); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["segment"] = [ex.Message] }); }
});
segments.MapPut("/{id:guid}", async (Guid id, UpdateCustomerSegmentRequest request, CustomerSegmentService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.UpdateAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["segment"] = [ex.Message] }); }
});
segments.MapDelete("/{id:guid}", async (Guid id, long expectedRevision, CustomerSegmentService service, CancellationToken ct) =>
{
    try { await service.DeleteAsync(id, expectedRevision, ct); return Results.NoContent(); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.NotFound(new { error = ex.Message }); }
});
api.MapGet("/crm/opportunities", async (Guid? customerId, CrmService crm, CancellationToken ct) => Results.Ok(await crm.GetOpportunitiesAsync(customerId, ct))).RequireAuthorization("CrmWrite");
api.MapPost("/crm/opportunities", async (CreateOpportunityRequest request, CrmService crm, CancellationToken ct) =>
{
    try { var opportunity = await crm.CreateOpportunityAsync(request, ct); return Results.Created($"/api/v1/crm/opportunities/{opportunity.Id}", opportunity); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["opportunity"] = [ex.Message] }); }
}).RequireAuthorization("CrmWrite");
api.MapPatch("/crm/opportunities/{id:guid}/stage", async (Guid id, MoveOpportunityRequest request, CrmService crm, CancellationToken ct) =>
{
    try { return Results.Ok(await crm.MoveOpportunityAsync(id, request.Stage, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["opportunity"] = [ex.Message] }); }
}).RequireAuthorization("CrmWrite");
api.MapGet("/crm/activities", async (Guid? customerId, bool? openTasksOnly, CrmService crm, CancellationToken ct) => Results.Ok(await crm.GetActivitiesAsync(customerId, openTasksOnly ?? false, ct))).RequireAuthorization("CrmWrite");
api.MapPost("/crm/activities", async (CreateCrmActivityRequest request, ClaimsPrincipal user, CrmService crm, CancellationToken ct) =>
{
    try { var activity = await crm.CreateActivityAsync(request, CurrentActorId(user), ct); return Results.Created($"/api/v1/crm/activities/{activity.Id}", activity); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["activity"] = [ex.Message] }); }
}).RequireAuthorization("CrmWrite");
api.MapPost("/crm/activities/{id:guid}/complete", async (Guid id, CrmService crm, CancellationToken ct) =>
{
    try { return Results.Ok(await crm.CompleteTaskAsync(id, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["task"] = [ex.Message] }); }
}).RequireAuthorization("CrmWrite");
var salesQuotes = api.MapGroup("/sales/quotes").RequireAuthorization("SalesQuoteManage");
salesQuotes.MapGet("/", async (SalesQuoteService service, CancellationToken ct) => Results.Ok(await service.GetAsync(ct)));
salesQuotes.MapGet("/{id:guid}", async (Guid id, SalesQuoteService service, CancellationToken ct) =>
    await service.GetByIdAsync(id, ct) is { } quote ? Results.Ok(quote) : Results.NotFound());
salesQuotes.MapPost("/", async (CreateSalesQuoteRequest request, SalesQuoteService service, CancellationToken ct) =>
{
    try { var quote = await service.CreateAsync(request, ct); return Results.Created($"/api/v1/sales/quotes/{quote.Id}", quote); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["quote"] = [ex.Message] }); }
});
salesQuotes.MapPut("/{id:guid}", async (Guid id, UpdateSalesQuoteRequest request, SalesQuoteService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.UpdateAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["quote"] = [ex.Message] }); }
});
salesQuotes.MapPost("/{id:guid}/send", async (Guid id, SalesQuoteStatusRequest request, SalesQuoteService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.MarkSentAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["quote"] = [ex.Message] }); }
});
salesQuotes.MapPost("/{id:guid}/accept", async (Guid id, SalesQuoteStatusRequest request, SalesQuoteService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.AcceptAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["quote"] = [ex.Message] }); }
});
salesQuotes.MapPost("/{id:guid}/reject", async (Guid id, SalesQuoteStatusRequest request, SalesQuoteService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.RejectAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["quote"] = [ex.Message] }); }
});
salesQuotes.MapPost("/{id:guid}/cancel", async (Guid id, SalesQuoteStatusRequest request, SalesQuoteService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.CancelAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["quote"] = [ex.Message] }); }
});
salesQuotes.MapPost("/{id:guid}/convert", async (Guid id, ConvertSalesQuoteRequest request, ClaimsPrincipal user, SalesQuoteService service, CancellationToken ct) =>
{
    try { var sale = await service.ConvertAsync(id, CurrentActorId(user), request, ct); return Results.Created($"/api/v1/sales/{sale.Id}", sale); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["quote"] = [ex.Message] }); }
}).RequireAuthorization("Checkout");
var parkedSales = api.MapGroup("/pos/parked-sales").RequireAuthorization("Checkout");
parkedSales.MapGet("/", async (string? registerCode, ParkedSaleService service, CancellationToken ct) =>
    Results.Ok(await service.GetParkedSalesAsync(registerCode ?? "MAIN", ct)));
parkedSales.MapPost("/", async (ParkSaleRequest request, ClaimsPrincipal user, ParkedSaleService service, CancellationToken ct) =>
{
    try { var parked = await service.ParkSaleAsync(request, CurrentActorId(user), ct); return Results.Created($"/api/v1/pos/parked-sales/{parked.Id}", parked); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["parkedSale"] = [ex.Message] }); }
});
parkedSales.MapPost("/{id:guid}/cancel", async (Guid id, CancelParkedSaleRequest request, ClaimsPrincipal user, ParkedSaleService service, CancellationToken ct) =>
{
    try { return Results.Ok(await service.CancelParkedSaleAsync(id, CurrentActorId(user), request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["parkedSale"] = [ex.Message] }); }
});
api.MapGet("/sales", async (int? limit, StoreService store, CancellationToken ct) => Results.Ok(await store.GetRecentSalesAsync(limit ?? 50, ct))).RequireAuthorization("SalesRead");
api.MapGet("/dashboard", async (StoreService store, CancellationToken ct) => Results.Ok(await store.GetDashboardAsync(ct)));
api.MapGet("/reports/sales", async (DateOnly from, DateOnly to, Guid? warehouseId, Guid? sellerUserId, string? documentNumber, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.GetSalesReportAsync(from, to, warehouseId, sellerUserId, documentNumber, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["period"] = [ex.Message] }); }
}).RequireAuthorization("ReportsRead");
api.MapGet("/reports/sales.csv", async (DateOnly from, DateOnly to, Guid? warehouseId, Guid? sellerUserId, string? documentNumber, StoreService store, CancellationToken ct) =>
{
    try
    {
        var report = await store.GetSalesReportAsync(from, to, warehouseId, sellerUserId, documentNumber, ct);
        var fileName = $"ojunny-ventas-{from:yyyyMMdd}-{to:yyyyMMdd}.csv";
        return Results.File(SalesReportCsvWriter.Write(report), "text/csv; charset=utf-8", fileName);
    }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["period"] = [ex.Message] }); }
}).RequireAuthorization("ReportsRead");
api.MapGet("/reports/payables-aging", async (DateOnly? asOf, StoreService store, CancellationToken ct) => Results.Ok(await store.GetPayablesAgingAsync(asOf, ct)))
    .RequireAuthorization("ReportsRead");
api.MapGet("/reports/receivables-aging", async (DateOnly? asOf, StoreService store, CancellationToken ct) => Results.Ok(await store.GetReceivablesAgingAsync(asOf, ct)))
    .RequireAuthorization("ReceivablesRead");
api.MapGet("/customers/{id:guid}/statement", async (Guid id, DateOnly from, DateOnly to, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.GetCustomerStatementAsync(id, from, to, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["statement"] = [ex.Message] }); }
}).RequireAuthorization("CustomerStatementRead");
api.MapGet("/customers/{id:guid}/statement.csv", async (Guid id, DateOnly from, DateOnly to, StoreService store, IBusinessClock clock, CancellationToken ct) =>
{
    try
    {
        var statement = await store.GetCustomerStatementAsync(id, from, to, ct);
        return Results.File(CustomerStatementCsvWriter.Write(statement, clock), "text/csv; charset=utf-8", $"ojunny-estado-cuenta-{to:yyyyMMdd}.csv");
    }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["statement"] = [ex.Message] }); }
}).RequireAuthorization("CustomerStatementRead");
api.MapPost("/sales/{id:guid}/receipts", async (Guid id, CustomerReceiptRequest request, ClaimsPrincipal user, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.RecordCustomerReceiptAsync(id, CurrentActorId(user), request, ct)); }
    catch (DomainException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization("ReceivablesRead");
api.MapPost("/sales/{id:guid}/returns", async (Guid id, CreateSaleReturnRequest request, ClaimsPrincipal user, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.ReturnSaleAsync(id, CurrentActorId(user), request, ct)); }
    catch (DomainException ex) { return Results.BadRequest(new { error = ex.Message }); }
}).RequireAuthorization("SalesReturn");
api.MapPost("/customers", async (CreateCustomerRequest request, StoreService store, CancellationToken ct) =>
{
    try { return Results.Created("/api/v1/customers", await store.CreateCustomerAsync(request.FirstName, request.LastName, request.Email, request.Phone, request.Company, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["customer"] = [ex.Message] }); }
}).RequireAuthorization("CrmWrite");
api.MapPut("/customers/{id:guid}", async (Guid id, UpdateCustomerRequest request, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.UpdateCustomerAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["customer"] = [ex.Message] }); }
}).RequireAuthorization("CrmWrite");
api.MapDelete("/customers/{id:guid}", async (Guid id, long expectedRevision, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.DeactivateCustomerAsync(id, expectedRevision, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["customer"] = [ex.Message] }); }
}).RequireAuthorization("CrmWrite");
api.MapPost("/products", async (CreateProductRequest request, StoreService store, CancellationToken ct) =>
{
    try { var product = await store.CreateProductAsync(request.Sku, request.Name, request.Category, request.UnitPrice, request.StockOnHand, request.Barcode, request.TaxRateOverride, request.InitialUnitCost, request.TaxProfileId, ct); return Results.Created($"/api/v1/products/{product.Id}", product); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["product"] = [ex.Message] }); }
}).RequireAuthorization("CatalogWrite");
api.MapPut("/products/{id:guid}", async (Guid id, UpdateProductRequest request, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.UpdateProductAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["product"] = [ex.Message] }); }
}).RequireAuthorization("CatalogWrite");
api.MapPatch("/products/{id:guid}/activation", async (Guid id, SetProductActiveRequest request, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.SetProductActiveAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["product"] = [ex.Message] }); }
}).RequireAuthorization("CatalogWrite");
api.MapPost("/pos/checkout", async (CheckoutRequest request, ClaimsPrincipal user, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.CheckoutAsync(request, CurrentActorId(user), ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["checkout"] = [ex.Message] }); }
}).RequireAuthorization("Checkout");
api.MapGet("/suppliers", async (string? search, StoreService store, CancellationToken ct) => Results.Ok(await store.SearchSuppliersAsync(search, ct))).RequireAuthorization("PurchasingWrite");
api.MapPost("/suppliers", async (CreateSupplierRequest request, StoreService store, CancellationToken ct) =>
{
    try { var supplier = await store.CreateSupplierAsync(request, ct); return Results.Created($"/api/v1/suppliers/{supplier.Id}", supplier); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["supplier"] = [ex.Message] }); }
}).RequireAuthorization("PurchasingWrite");
api.MapPut("/suppliers/{id:guid}", async (Guid id, UpdateSupplierRequest request, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.UpdateSupplierAsync(id, request, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["supplier"] = [ex.Message] }); }
}).RequireAuthorization("PurchasingWrite");
api.MapDelete("/suppliers/{id:guid}", async (Guid id, long expectedRevision, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.DeactivateSupplierAsync(id, expectedRevision, ct)); }
    catch (ConcurrencyConflictException ex) { return Results.Conflict(new { error = ex.Message }); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["supplier"] = [ex.Message] }); }
}).RequireAuthorization("PurchasingWrite");
api.MapGet("/purchases", async (StoreService store, CancellationToken ct) => Results.Ok(await store.GetPurchaseOrdersAsync(ct))).RequireAuthorization("PurchasingWrite");
api.MapPost("/purchases", async (CreatePurchaseOrderRequest request, StoreService store, CancellationToken ct) =>
{
    try { var purchase = await store.CreatePurchaseOrderAsync(request, ct); return Results.Created($"/api/v1/purchases/{purchase.Id}", purchase); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["purchase"] = [ex.Message] }); }
}).RequireAuthorization("PurchasingWrite");
api.MapPost("/purchases/{id:guid}/receive", async (Guid id, ReceivePurchaseOrderRequest request, ClaimsPrincipal user, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.ReceivePurchaseOrderAsync(id, CurrentActorId(user), request, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["receipt"] = [ex.Message] }); }
}).RequireAuthorization("PurchasingWrite");
api.MapPost("/purchases/{id:guid}/payments", async (Guid id, CreateSupplierPaymentRequest request, ClaimsPrincipal user, StoreService store, CancellationToken ct) =>
{
    try { var order = await store.RecordSupplierPaymentAsync(id, CurrentActorId(user), request, ct); return Results.Ok(order); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["payment"] = [ex.Message] }); }
}).RequireAuthorization("SupplierPayments");
api.MapPost("/purchases/{id:guid}/cancel", async (Guid id, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.CancelPurchaseOrderAsync(id, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["purchase"] = [ex.Message] }); }
}).RequireAuthorization("PurchasingWrite");

api.MapGet("/cash/session", async (string? registerCode, StoreService store, CancellationToken ct) => Results.Ok(await store.GetCurrentCashSessionAsync(registerCode ?? "MAIN", ct)))
    .RequireAuthorization("CashManagement");
api.MapGet("/cash/sessions", async (int? limit, StoreService store, CancellationToken ct) => Results.Ok(await store.GetRecentCashSessionsAsync(limit ?? 10, ct)))
    .RequireAuthorization("CashManagement");
api.MapPost("/cash/sessions", async (OpenCashSessionRequest request, ClaimsPrincipal user, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.OpenCashSessionAsync(CurrentActorId(user), request, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["cashSession"] = [ex.Message] }); }
}).RequireAuthorization("CashManagement");
api.MapPost("/cash/sessions/{id:guid}/movements", async (Guid id, CashMovementRequest request, ClaimsPrincipal user, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.RecordCashMovementAsync(id, CurrentActorId(user), request, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["movement"] = [ex.Message] }); }
}).RequireAuthorization("CashManagement");
api.MapPost("/cash/sessions/{id:guid}/close", async (Guid id, CloseCashSessionRequest request, ClaimsPrincipal user, StoreService store, CancellationToken ct) =>
{
    try { return Results.Ok(await store.CloseCashSessionAsync(id, CurrentActorId(user), request, ct)); }
    catch (DomainException ex) { return Results.ValidationProblem(new Dictionary<string, string[]> { ["cashSession"] = [ex.Message] }); }
}).RequireAuthorization("CashManagement");
api.MapGet("/sales/{id:guid}/receipt", async (Guid id, StoreService store, CancellationToken ct) =>
{
    var receipt = await store.GetSaleReceiptAsync(id, ct);
    return receipt is null ? Results.NotFound() : Results.Ok(receipt);
}).RequireAuthorization("SalesRead");

Guid CurrentActorId(ClaimsPrincipal principal) => Guid.TryParse(principal.FindFirstValue(ClaimTypes.NameIdentifier), out var id) ? id : Guid.Empty;

if (builder.Configuration.GetValue<bool>("Database:ApplyMigrationsOnStartup"))
    await DatabaseInitializer.InitializeAsync(app.Services);
await IdentityInitializer.InitializeAsync(app.Services, app.Configuration);

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(typeof(OJunnySyS.PosClient.Pages.PosClientPage).Assembly);

app.Run();

public sealed record CreateCustomerRequest(string FirstName, string LastName, string Email, string Phone, string? Company);
public sealed record CreateProductRequest(string Sku, string Name, string Category, decimal UnitPrice, int StockOnHand, string? Barcode, decimal? TaxRateOverride = null, decimal? InitialUnitCost = null, Guid? TaxProfileId = null);
