using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Pos;
using OJunnySyS.Infrastructure.Identity;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfParkedSaleRepository(IDbContextFactory<StoreDbContext> contextFactory, ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess) : IParkedSaleRepository
{
    public async Task<IReadOnlyList<ParkedSale>> GetParkedSalesAsync(string registerCode, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        registerCode = PosRegister.NormalizeCode(registerCode);
        return await db.ParkedSales.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.RegisterCode == registerCode && x.Status == ParkedSaleStatus.Parked)
            .OrderBy(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);
    }

    public async Task<ParkedSale> ParkSaleAsync(ParkSaleRequest request, Guid cashierUserId, CancellationToken cancellationToken = default)
    {
        if (request.IdempotencyKey == Guid.Empty || cashierUserId == Guid.Empty || request.Lines.Count is < 1 or > 200)
            throw new DomainException("La venta en espera debe incluir una clave, cajero y hasta 200 productos.");
        if (request.Lines.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0 || x.DiscountPercent is < 0 or > 100) ||
            request.Lines.Select(x => x.ProductId).Distinct().Count() != request.Lines.Count)
            throw new DomainException("Revisa los artículos, cantidades y descuentos antes de guardar la venta en espera.");
        var registerCode = PosRegister.NormalizeCode(request.RegisterCode);
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var existing = await db.ParkedSales.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existing is not null)
        {
            EnsureSameParkRequest(existing, request, cashierUserId, registerCode);
            await transaction.CommitAsync(cancellationToken);
            return existing;
        }

        var cashier = await db.Users.SingleOrDefaultAsync(x => x.Id == cashierUserId && x.TenantId == tenantId &&
            (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow), cancellationToken)
            ?? throw new DomainException("El cajero no está activo en esta empresa.");
        var register = await db.PosRegisters.SingleOrDefaultAsync(x => x.Code == registerCode && x.IsActive, cancellationToken)
            ?? throw new DomainException("El terminal seleccionado no existe o está inactivo.");
        string customerName = "Consumidor final";
        if (request.CustomerId is { } customerId)
        {
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == customerId && x.IsActive, cancellationToken)
                ?? throw new DomainException("El cliente no existe o está inactivo en esta empresa.");
            customerName = customer.FullName;
        }

        string? sellerName = null;
        if (request.SellerUserId is { } sellerId)
        {
            var seller = await db.Users.SingleOrDefaultAsync(x => x.Id == sellerId && x.TenantId == tenantId &&
                (x.LockoutEnd == null || x.LockoutEnd <= DateTimeOffset.UtcNow), cancellationToken)
                ?? throw new DomainException("El vendedor seleccionado no está activo en esta empresa.");
            var sellerRoles = new[] { AppRoles.Administrator, AppRoles.Manager, AppRoles.Salesperson };
            if (!await (from userRole in db.UserRoles join role in db.Roles on userRole.RoleId equals role.Id
                        where userRole.UserId == seller.Id && role.Name != null && sellerRoles.Contains(role.Name)
                        select role.Id).AnyAsync(cancellationToken))
                throw new DomainException("La cuenta seleccionada no puede asignarse como vendedor.");
            sellerName = seller.DisplayName;
        }

        if (request.Lines.Any(x => x.DiscountPercent > 0))
        {
            var discountRoles = new[] { AppRoles.Administrator, AppRoles.Manager };
            if (!await (from userRole in db.UserRoles join role in db.Roles on userRole.RoleId equals role.Id
                        where userRole.UserId == cashier.Id && role.Name != null && discountRoles.Contains(role.Name)
                        select role.Id).AnyAsync(cancellationToken))
                throw new DomainException("Solo un administrador o gerente puede autorizar descuentos.");
        }

        var productIds = request.Lines.Select(x => x.ProductId).ToArray();
        var products = await db.Products.Where(x => productIds.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, cancellationToken);
        if (products.Count != productIds.Length) throw new DomainException("Uno o más productos ya no están activos en esta empresa.");
        var stockRows = await db.WarehouseStocks.Where(x => x.WarehouseId == register.WarehouseId && productIds.Contains(x.ProductId))
            .ToDictionaryAsync(x => x.ProductId, cancellationToken);
        foreach (var line in request.Lines)
        {
            if (!stockRows.TryGetValue(line.ProductId, out var stock)) throw new DomainException("No hay saldo de inventario para uno de los productos en este terminal.");
            if (line.Quantity > stock.Quantity) throw new DomainException($"La cantidad de {products[line.ProductId].Name} supera el stock disponible al guardar.");
        }

        var parkedId = Guid.NewGuid();
        var parkedLines = request.Lines.OrderBy(x => x.ProductId).Select(line => new ParkedSaleLine(tenantId, Guid.NewGuid(), parkedId,
            line.ProductId, products[line.ProductId].Sku, products[line.ProductId].Name, line.Quantity, line.DiscountPercent)).ToArray();
        var parked = new ParkedSale(tenantId, parkedId, request.IdempotencyKey, registerCode, cashierUserId,
            request.CustomerId, customerName, request.SellerUserId, sellerName, parkedLines);
        db.ParkedSales.Add(parked);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return parked;
    }

    public async Task<ParkedSale> CancelParkedSaleAsync(Guid id, Guid actorUserId, CancelParkedSaleRequest request,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty || actorUserId == Guid.Empty) throw new DomainException("La venta en espera y el usuario son obligatorios.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var parked = await db.ParkedSales.FromSqlInterpolated($"SELECT * FROM ParkedSales WHERE TenantId = {tenantId} AND Id = {id} FOR UPDATE")
            .Include(x => x.Lines).SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La venta en espera no existe en esta empresa.");
        parked.Cancel(actorUserId, request.ExpectedRevision);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return parked;
    }

    private static void EnsureSameParkRequest(ParkedSale existing, ParkSaleRequest request, Guid cashierUserId, string registerCode)
    {
        var requestedLines = request.Lines.OrderBy(x => x.ProductId).Select(x => (x.ProductId, x.Quantity, x.DiscountPercent)).ToArray();
        var savedLines = existing.Lines.OrderBy(x => x.ProductId).Select(x => (x.ProductId, x.Quantity, x.DiscountPercent)).ToArray();
        if (existing.CashierUserId != cashierUserId || existing.RegisterCode != registerCode || existing.CustomerId != request.CustomerId ||
            existing.SellerUserId != request.SellerUserId || !requestedLines.SequenceEqual(savedLines))
            throw new DomainException("La clave ya se utilizó para guardar una venta en espera con otros datos.");
    }

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        return db;
    }
}
