using Microsoft.EntityFrameworkCore;
using OJunnySyS.Domain.Accounting;
using OJunnySyS.Infrastructure.Persistence;

namespace OJunnySyS.Infrastructure.Identity;

public static class ChartAccountDefaults
{
    private static readonly (string Code, string Name, AccountType Type)[] Accounts =
    [
        ("1000", "Caja y bancos", AccountType.Asset),
        ("1010", "Bancos", AccountType.Asset),
        ("1020", "Tarjetas por liquidar", AccountType.Asset),
        ("1030", "Otros medios por liquidar", AccountType.Asset),
        ("1100", "Cuentas por cobrar", AccountType.Asset),
        ("1200", "Inventario", AccountType.Asset),
        ("1300", "Impuestos acreditables", AccountType.Asset),
        ("2000", "Cuentas por pagar", AccountType.Liability),
        ("2100", "Impuestos por pagar", AccountType.Liability),
        ("3000", "Capital", AccountType.Equity),
        ("4000", "Ingresos por ventas", AccountType.Revenue),
        ("4100", "Otros ingresos", AccountType.Revenue),
        ("5000", "Costo de ventas", AccountType.Expense),
        ("6000", "Gastos operativos", AccountType.Expense)
    ];

    public static async Task EnsureAsync(StoreDbContext db, Guid tenantId, CancellationToken cancellationToken = default)
    {
        var existing = await db.ChartAccounts.Select(x => x.Code).ToListAsync(cancellationToken);
        var missing = Accounts.Where(x => !existing.Contains(x.Code))
            .Select(x => new ChartAccount(tenantId, x.Code, x.Name, x.Type, isSystem: true));
        db.ChartAccounts.AddRange(missing);
        await db.SaveChangesAsync(cancellationToken);
    }
}
