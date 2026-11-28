using OJunnySyS.Application.Abstractions;
using OJunnySyS.Domain.Tenancy;
using Microsoft.AspNetCore.Mvc;

namespace OJunnySyS.Web.Identity;

public sealed class TenantAccessMiddleware(RequestDelegate next)
{
    public async Task InvokeAsync(HttpContext context, TenantContext tenantContext, ITenantAccessGuard accessGuard)
    {
        var tenantRoute = context.Request.Path.StartsWithSegments("/app") ||
                          context.Request.Path.StartsWithSegments("/api") ||
                          context.Request.Path.StartsWithSegments("/_blazor");
        if (!tenantRoute)
        {
            await next(context);
            return;
        }

        if (tenantContext.CurrentTenantId is not { } tenantId)
        {
            if (context.User.Identity?.IsAuthenticated == true && context.User.IsInRole("PlatformAdministrator"))
            {
                if (context.Request.Path.StartsWithSegments("/app"))
                {
                    context.Response.Redirect("/platform/tenants");
                    return;
                }

                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    await context.Response.WriteAsJsonAsync(new ProblemDetails
                    {
                        Status = StatusCodes.Status403Forbidden,
                        Title = "Selecciona una empresa",
                        Detail = "La administración central no opera módulos de empresa. Abre un espacio desde Administración de plataforma."
                    }, context.RequestAborted);
                    return;
                }
            }

            if (context.User.Identity?.IsAuthenticated == true && context.Request.Path.StartsWithSegments("/app"))
            {
                context.Response.Redirect("/account/tenant-unavailable");
                return;
            }

            if (context.User.Identity?.IsAuthenticated == true && context.Request.Path.StartsWithSegments("/api"))
            {
                context.Response.StatusCode = StatusCodes.Status403Forbidden;
                await context.Response.WriteAsJsonAsync(new ProblemDetails
                {
                    Status = StatusCodes.Status403Forbidden,
                    Title = "No hay una empresa activa",
                    Detail = "La cuenta no tiene una empresa activa asociada. Contacta al administrador de la plataforma."
                }, context.RequestAborted);
                return;
            }

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
