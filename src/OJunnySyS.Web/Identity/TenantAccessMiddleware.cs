using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace OJunnySyS.Web.Identity;

public sealed class TenantAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, ITenantAccessGuard accessGuard)
    {
        if (tenantContext.CurrentTenantId is not { } tenantId ||
            !(context.Request.Path.StartsWithSegments("/app") || context.Request.Path.StartsWithSegments("/api") || context.Request.Path.StartsWithSegments("/_blazor")))
        {
            await next(context);
            return;
        }

        try
        {
            var userId = Guid.TryParse(context.User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value, out var parsedUserId) ? parsedUserId : (Guid?)null;
            await accessGuard.EnsureCanOperateAsync(tenantId, userId, context.RequestAborted);
            await next(context);
        }
        catch (TenantAccessException ex)
        {
            if (context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status423Locked;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status423Locked,
                    Title = "Espacio de empresa no disponible",
                    Detail = ex.Message
                }, context.RequestAborted);
                return;
            }

            context.Response.Redirect("/account/tenant-unavailable");
        }
    }
}
