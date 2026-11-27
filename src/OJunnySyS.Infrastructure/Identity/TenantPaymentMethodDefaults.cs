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
        var accounts = await db.ChartAccounts.Where(x => x.TenantId == tenantId && x.IsActive)
            .ToDictionaryAsync(x => x.Code, cancellationToken);
        var methods = await db.TenantPaymentMethods.Where(x => x.TenantId == tenantId).ToListAsync(cancellationToken);
        foreach (var definition in Defaults)
        {
            var accountCode = LedgerAccountCode(definition.Type);
            if (!accounts.TryGetValue(accountCode, out var account))
                throw new InvalidOperationException($"No existe la cuenta contable base {accountCode} para configurar los medios de pago.");

            var method = methods.SingleOrDefault(x => x.Code == definition.Code);
            if (method is null)
            {
                method = new TenantPaymentMethod(tenantId, definition.Code, definition.Name, definition.Type, ledgerAccountId: account.Id);
                db.TenantPaymentMethods.Add(method);
                methods.Add(method);
            }
            else if (method.LedgerAccountId is null)
            {
                method.Update(method.Name, method.Instructions, method.AccountLabel, method.AccountReference, method.QrPayload,
                    account.Id, method.Revision);
            }
        }
        await db.SaveChangesAsync(cancellationToken);
    }

    public static string LedgerAccountCode(PaymentMethodType type) => type switch
    {
        PaymentMethodType.Cash => "1000",
        PaymentMethodType.Card => "1020",
        PaymentMethodType.Transfer => "1010",
        _ => "1030"
    };
}
