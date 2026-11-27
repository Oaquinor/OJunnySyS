using Microsoft.EntityFrameworkCore;
using OJunnySyS.Domain.Sales;
using OJunnySyS.Infrastructure.Persistence;

namespace OJunnySyS.Infrastructure.Identity;

public static class TenantPaymentMethodDefaults
{
    private static readonly (string Code, string Name, PaymentMethodType Type)[] Defaults =
    [
        ("cash", "Efectivo", PaymentMethodType.Cash),
        ("card", "Tarjeta", PaymentMethodType.Card),
        ("transfer", "Transferencia", PaymentMethodType.Transfer),
        ("other", "Otro", PaymentMethodType.Other)
    ];

    public static async Task EnsureAsync(StoreDbContext db, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var existing = await db.TenantPaymentMethods.Where(x => x.TenantId == tenantId).Select(x => x.Code).ToListAsync(cancellationToken);
        foreach (var method in Defaults.Where(x => !existing.Contains(x.Code, StringComparer.OrdinalIgnoreCase)))
            db.TenantPaymentMethods.Add(new TenantPaymentMethod(tenantId, method.Code, method.Name, method.Type));
        if (Defaults.Any(x => !existing.Contains(x.Code, StringComparer.OrdinalIgnoreCase)))
            await db.SaveChangesAsync(cancellationToken);
    }
}
