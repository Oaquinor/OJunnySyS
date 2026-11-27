using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Purchasing;
using OJunnySyS.Domain.Cash;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfStoreRepository(IDbContextFactory<StoreDbContext> contextFactory, ITenantContext tenantContext, ITenantAccessGuard tenantAccess, ILogger<EfStoreRepository> logger) : IStoreRepository
{
    public async Task<IReadOnlyList<Product>> SearchProductsAsync(string? query, bool includeInactive = false, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var products = db.Products.AsNoTracking();
        if (!includeInactive) products = products.Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(query)) products = products.Where(x => x.Name.Contains(query) || x.Sku.Contains(query) || (x.Barcode != null && x.Barcode.Contains(query)));
        return await products.OrderBy(x => x.Name).Take(100).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Customer>> SearchCustomersAsync(string? query, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var customers = db.Customers.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(query)) customers = customers.Where(x => x.FirstName.Contains(query) || x.LastName.Contains(query) || x.Email.Contains(query) || (x.Company != null && x.Company.Contains(query)));
        return await customers.OrderBy(x => x.FirstName).ThenBy(x => x.LastName).Take(100).ToListAsync(cancellationToken);
    }

    public async Task<Customer?> GetCustomerAsync(Guid customerId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        return await db.Customers.AsNoTracking().SingleOrDefaultAsync(x => x.Id == customerId, cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> GetCustomerSalesAsync(Guid customerId, int count = 100, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        return await db.Sales.AsNoTracking().Where(x => x.CustomerId == customerId)
            .OrderByDescending(x => x.CreatedAtUtc).Take(Math.Clamp(count, 1, 200)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> GetRecentSalesAsync(int count, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        return await db.Sales.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(Math.Clamp(count, 1, 100)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> GetSalesInPeriodAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        return await db.Sales.AsNoTracking().Include(x => x.Lines)
            .Where(x => x.CreatedAtUtc >= from && x.CreatedAtUtc < to).ToListAsync(cancellationToken);
    }

    public async Task<Customer> CreateCustomerAsync(string firstName, string lastName, string email, string phone, string? company, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        var customer = new Customer(tenantId, firstName, lastName, email, phone, company);
        await using var db = await CreateDbContextAsync(cancellationToken);
        if (await db.Customers.AnyAsync(x => x.Email == customer.Email, cancellationToken))
            throw new DomainException("Ya existe un cliente con ese correo.");
        db.Customers.Add(customer);
        await db.SaveChangesAsync(cancellationToken);
        return customer;
    }

    public async Task<Customer> UpdateCustomerAsync(Guid customerId, UpdateCustomerRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == customerId && x.IsActive, cancellationToken)
            ?? throw new DomainException("El cliente no existe o está inactivo.");
        if (customer.Revision != request.ExpectedRevision)
            throw new ConcurrencyConflictException("El cliente fue modificado por otra persona. Recarga la lista y vuelve a intentarlo.");
        customer.UpdateContact(request.FirstName, request.LastName, request.Email, request.Phone, request.Company);
        if (await db.Customers.AnyAsync(x => x.Id != customerId && x.Email == customer.Email, cancellationToken))
            throw new DomainException("Ya existe un cliente con ese correo.");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El cliente fue modificado por otra persona. Recarga la lista y vuelve a intentarlo."); }
        return customer;
    }

    public async Task<Customer> DeactivateCustomerAsync(Guid customerId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == customerId && x.IsActive, cancellationToken)
            ?? throw new DomainException("El cliente no existe o ya está inactivo.");
        if (customer.Revision != expectedRevision)
            throw new ConcurrencyConflictException("El cliente fue modificado por otra persona. Recarga la lista y vuelve a intentarlo.");
        customer.Deactivate();
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El cliente fue modificado por otra persona. Recarga la lista y vuelve a intentarlo."); }
        return customer;
    }

    public async Task<Product> CreateProductAsync(string sku, string name, string category, decimal unitPrice, int stock, string? barcode, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        var product = new Product(tenantId, sku, name, category, unitPrice, stock, barcode);
        await using var db = await CreateDbContextAsync(cancellationToken);
        if (await db.Products.AnyAsync(x => x.Sku == product.Sku || (product.Barcode != null && x.Barcode == product.Barcode), cancellationToken))
            throw new DomainException("Ya existe un producto con ese SKU o código de barras.");
        db.Products.Add(product);
        if (stock > 0) db.InventoryMovements.Add(new InventoryMovement(tenantId, product.Id, InventoryMovementType.StockAdjustment, stock, stock, "Existencias iniciales del producto"));
        await db.SaveChangesAsync(cancellationToken);
        return product;
    }

    public async Task<Product> UpdateProductAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new DomainException("El producto no existe en esta empresa.");
        if (product.Revision != request.ExpectedRevision)
            throw new ConcurrencyConflictException("El producto cambió en otra sesión. Recarga el catálogo antes de guardar.");
        product.UpdateCatalog(request.Sku, request.Name, request.Category, request.UnitPrice, request.Barcode);
        if (await db.Products.AnyAsync(x => x.Id != productId && (x.Sku == product.Sku || (product.Barcode != null && x.Barcode == product.Barcode)), cancellationToken))
            throw new DomainException("Ya existe un producto con ese SKU o código de barras.");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El producto cambió en otra sesión. Recarga el catálogo antes de guardar."); }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "No se pudo actualizar el producto {ProductId} por una clave única duplicada.", productId);
            throw new DomainException("Ya existe un producto con ese SKU o código de barras.");
        }
        return product;
    }

    public async Task<Product> SetProductActiveAsync(Guid productId, SetProductActiveRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new DomainException("El producto no existe en esta empresa.");
        if (product.Revision != request.ExpectedRevision)
            throw new ConcurrencyConflictException("El producto cambió en otra sesión. Recarga el catálogo antes de actualizar su estado.");
        if (request.IsActive) product.Reactivate(); else product.Deactivate();
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El producto cambió en otra sesión. Recarga el catálogo antes de actualizar su estado."); }
        return product;
    }

    public async Task<Sale> CheckoutAsync(CheckoutRequest request, Guid cashierUserId, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0) throw new DomainException("Agrega productos antes de cobrar.");
        if (request.Lines.Count > 200 || request.Lines.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0)) throw new DomainException("La venta debe tener hasta 200 líneas con productos y cantidades válidas.");
        if (request.IdempotencyKey == Guid.Empty) throw new DomainException("La clave de idempotencia del cobro es obligatoria.");
        if (request.PaymentMethod == PaymentMethod.Credit) throw new DomainException("Las ventas a crédito aún no están habilitadas.");
        if (cashierUserId == Guid.Empty) throw new DomainException("No se pudo identificar al cajero.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var priorSale = await db.Sales.AsNoTracking().Include(x => x.Lines)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (priorSale is not null)
        {
            EnsureSameCheckout(priorSale, request, cashierUserId);
            await transaction.CommitAsync(cancellationToken);
            return priorSale;
        }
        var cashSession = await db.CashSessions.FromSqlInterpolated($"SELECT * FROM CashSessions WHERE TenantId = {tenantId} AND RegisterCode = {"MAIN"} AND Status = {(int)CashSessionStatus.Open} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("Abre la caja principal antes de registrar ventas.");
        priorSale = await LoadSaleByCheckoutKeyAsync(db, tenantId, request.IdempotencyKey, cancellationToken);
        if (priorSale is not null)
        {
            EnsureSameCheckout(priorSale, request, cashierUserId);
            await transaction.CommitAsync(cancellationToken);
            return priorSale;
        }
        var customer = request.CustomerId is { } id
            ? await db.Customers.FromSqlInterpolated($"SELECT * FROM Customers WHERE Id = {id} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            : null;
        if (request.CustomerId is not null && customer is null) throw new DomainException("El cliente seleccionado no existe o está inactivo.");

        var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new DomainException("No se encontró la empresa activa.");
        var lines = new List<SaleLine>();
        var stockMovements = new List<(Product Product, int Quantity)>();
        foreach (var groupedLine in request.Lines.GroupBy(x => x.ProductId).OrderBy(x => x.Key))
        {
            var quantity = SumQuantities(groupedLine.Select(x => x.Quantity));
            if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor que cero.");
            var product = await db.Products.FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {groupedLine.Key} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("Uno de los productos ya no está disponible.");
            product.AdjustStock(-quantity);
            stockMovements.Add((product, quantity));
            lines.Add(new SaleLine(product.Id, product.Sku, product.Name, product.UnitPrice, quantity));
        }

        var sale = new Sale(tenantId, $"V-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}", customer, request.PaymentMethod, lines, cashSession.Id, cashierUserId, tenant.DefaultTaxRate, request.IdempotencyKey);
        foreach (var movement in stockMovements)
            db.InventoryMovements.Add(new InventoryMovement(tenantId, movement.Product.Id, InventoryMovementType.Sale, -movement.Quantity, movement.Product.StockOnHand, sale.Number));
        customer?.RecordPurchase(sale.Total);
        db.Sales.Add(sale);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return sale;
    }

    private static async Task<Sale?> LoadSaleByCheckoutKeyAsync(StoreDbContext db, Guid tenantId, Guid idempotencyKey, CancellationToken ct) =>
        await db.Sales.FromSqlInterpolated($"SELECT * FROM Sales WHERE TenantId = {tenantId} AND IdempotencyKey = {idempotencyKey} FOR UPDATE")
            .AsSplitQuery().Include(x => x.Lines).SingleOrDefaultAsync(ct);

    private static void EnsureSameCheckout(Sale sale, CheckoutRequest request, Guid cashierUserId)
    {
        var requestedLines = request.Lines.GroupBy(x => x.ProductId).ToDictionary(x => x.Key, x => SumQuantities(x.Select(line => line.Quantity)));
        var storedLines = sale.Lines.ToDictionary(x => x.ProductId, x => x.Quantity);
        if (sale.CashierUserId != cashierUserId || sale.CustomerId != request.CustomerId || sale.PaymentMethod != request.PaymentMethod
            || requestedLines.Count != storedLines.Count || requestedLines.Any(x => !storedLines.TryGetValue(x.Key, out var quantity) || quantity != x.Value))
            throw new DomainException("La clave de idempotencia ya se utilizó con otros datos de cobro.");
    }

    public async Task<CashSessionDto?> GetCurrentCashSessionAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var session = await db.CashSessions.AsNoTracking()
            .Where(x => x.RegisterCode == "MAIN" && x.Status == CashSessionStatus.Open)
            .OrderByDescending(x => x.CreatedAtUtc).FirstOrDefaultAsync(cancellationToken);
        return session is null ? null : await MapCashSessionAsync(db, session, cancellationToken);
    }

    public async Task<IReadOnlyList<CashSessionDto>> GetRecentCashSessionsAsync(int count, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var sessions = await db.CashSessions.AsNoTracking().OrderByDescending(x => x.CreatedAtUtc).Take(Math.Clamp(count, 1, 50)).ToListAsync(cancellationToken);
        var result = new List<CashSessionDto>(sessions.Count);
        foreach (var session in sessions) result.Add(await MapCashSessionAsync(db, session, cancellationToken));
        return result;
    }

    public async Task<IReadOnlyList<CashSessionDto>> GetCashSessionsInPeriodAsync(DateTimeOffset from, DateTimeOffset to, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var sessions = await db.CashSessions.AsNoTracking().Where(x => x.CreatedAtUtc >= from && x.CreatedAtUtc < to)
            .OrderByDescending(x => x.CreatedAtUtc).Take(5000).ToListAsync(cancellationToken);
        var result = new List<CashSessionDto>(sessions.Count);
        foreach (var session in sessions) result.Add(await MapCashSessionAsync(db, session, cancellationToken));
        return result;
    }

    public async Task<CashSessionDto> OpenCashSessionAsync(Guid userId, OpenCashSessionRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La empresa activa no existe.");
        var registerCode = string.IsNullOrWhiteSpace(request.RegisterCode) ? "MAIN" : request.RegisterCode.Trim().ToUpperInvariant();
        if (registerCode != "MAIN") throw new DomainException("Por ahora, la empresa tiene configurada la caja principal (MAIN).");
        if (await db.CashSessions.AnyAsync(x => x.Status == CashSessionStatus.Open, cancellationToken))
            throw new DomainException("Esta caja ya tiene un turno abierto.");
        var session = new CashSession(tenantId, registerCode, request.OpeningFloat, userId);
        db.CashSessions.Add(session);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapCashSessionAsync(db, session, cancellationToken);
    }

    public async Task<CashSessionDto> RecordCashMovementAsync(Guid sessionId, Guid userId, CashMovementRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.CashSessions.FromSqlInterpolated($"SELECT * FROM CashSessions WHERE Id = {sessionId} AND TenantId = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La sesión de caja no existe.");
        if (session.Status != CashSessionStatus.Open) throw new DomainException("La caja está cerrada y no admite movimientos.");
        db.CashMovements.Add(new CashMovement(tenantId, session.Id, userId, request.Kind, request.Amount, request.Description));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapCashSessionAsync(db, session, cancellationToken);
    }

    public async Task<CashSessionDto> CloseCashSessionAsync(Guid sessionId, Guid userId, decimal countedCash, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var session = await db.CashSessions.FromSqlInterpolated($"SELECT * FROM CashSessions WHERE Id = {sessionId} AND TenantId = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La sesión de caja no existe.");
        if (session.Status != CashSessionStatus.Open) throw new DomainException("La caja ya está cerrada.");
        var sales = await db.Sales.AsNoTracking().Where(x => x.CashSessionId == session.Id && x.PaymentMethod == PaymentMethod.Cash && x.Status == SaleStatus.Completed).SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;
        var cashIn = await db.CashMovements.AsNoTracking().Where(x => x.SessionId == session.Id && x.Kind == CashMovementKind.CashIn).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var cashOut = await db.CashMovements.AsNoTracking().Where(x => x.SessionId == session.Id && x.Kind == CashMovementKind.CashOut).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        session.Close(userId, session.OpeningFloat + sales + cashIn - cashOut, countedCash);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return await MapCashSessionAsync(db, session, cancellationToken);
    }

    public async Task<SaleReceiptDto?> GetSaleReceiptAsync(Guid saleId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var sale = await db.Sales.AsNoTracking().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == saleId, cancellationToken);
        if (sale is null) return null;
        var registerCode = sale.CashSessionId is { } sessionId
            ? await db.CashSessions.AsNoTracking().Where(x => x.Id == sessionId).Select(x => x.RegisterCode).SingleOrDefaultAsync(cancellationToken)
            : null;
        return new SaleReceiptDto(sale.Id, sale.Number, sale.CustomerName, sale.Subtotal, sale.TaxRate, sale.TaxAmount, sale.Total,
            sale.PaymentMethod, sale.Status, sale.CreatedAtUtc, registerCode,
            sale.Lines.Select(x => new ReceiptLineDto(x.Sku, x.ProductName, x.Quantity, x.UnitPrice, x.LineTotal)).ToArray());
    }

    private static async Task<CashSessionDto> MapCashSessionAsync(StoreDbContext db, CashSession session, CancellationToken cancellationToken)
    {
        var sales = await db.Sales.AsNoTracking().Where(x => x.CashSessionId == session.Id && x.PaymentMethod == PaymentMethod.Cash && x.Status == SaleStatus.Completed).SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;
        var cashIn = await db.CashMovements.AsNoTracking().Where(x => x.SessionId == session.Id && x.Kind == CashMovementKind.CashIn).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var cashOut = await db.CashMovements.AsNoTracking().Where(x => x.SessionId == session.Id && x.Kind == CashMovementKind.CashOut).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var movements = await db.CashMovements.AsNoTracking().Where(x => x.SessionId == session.Id).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);
        var expected = session.ExpectedCash ?? decimal.Round(session.OpeningFloat + sales + cashIn - cashOut, 2);
        return new CashSessionDto(session.Id, session.RegisterCode, session.Status, session.OpeningFloat, session.OpenedByUserId, session.OpenedAtUtc,
            sales, cashIn, cashOut, expected, session.ClosedAtUtc, session.CountedCash, session.Difference,
            movements.Select(x => new CashMovementDto(x.Id, x.Kind, x.Amount, x.Description, x.UserId, x.CreatedAtUtc)).ToArray());
    }

    public async Task<IReadOnlyList<Supplier>> SearchSuppliersAsync(string? query, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var suppliers = db.Suppliers.AsNoTracking().Where(x => x.IsActive);
        if (!string.IsNullOrWhiteSpace(query)) suppliers = suppliers.Where(x => x.Name.Contains(query) || x.Email.Contains(query) || (x.TaxIdentifier != null && x.TaxIdentifier.Contains(query)));
        return await suppliers.OrderBy(x => x.Name).Take(100).ToListAsync(cancellationToken);
    }

    public async Task<Supplier> CreateSupplierAsync(CreateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        var supplier = new Supplier(tenantId, request.Name, request.TaxIdentifier, request.Email, request.Phone, request.Notes);
        await using var db = await CreateDbContextAsync(cancellationToken);
        if (supplier.TaxIdentifier is not null && await db.Suppliers.AnyAsync(x => x.TaxIdentifier == supplier.TaxIdentifier, cancellationToken))
            throw new DomainException("Ya existe un proveedor con ese identificador fiscal.");
        db.Suppliers.Add(supplier);
        await db.SaveChangesAsync(cancellationToken);
        return supplier;
    }

    public async Task<Supplier> UpdateSupplierAsync(Guid supplierId, UpdateSupplierRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == supplierId && x.IsActive, cancellationToken)
            ?? throw new DomainException("El proveedor no existe o está inactivo.");
        if (supplier.Revision != request.ExpectedRevision)
            throw new ConcurrencyConflictException("El proveedor fue modificado por otra persona. Recarga la lista e inténtalo de nuevo.");
        supplier.UpdateContact(request.Name, request.TaxIdentifier, request.Email, request.Phone, request.Notes);
        if (supplier.TaxIdentifier is not null && await db.Suppliers.AnyAsync(x => x.Id != supplierId && x.TaxIdentifier == supplier.TaxIdentifier, cancellationToken))
            throw new DomainException("Ya existe un proveedor con ese identificador fiscal.");
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El proveedor fue modificado por otra persona. Recarga la lista e inténtalo de nuevo."); }
        return supplier;
    }

    public async Task<Supplier> DeactivateSupplierAsync(Guid supplierId, long expectedRevision, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var supplier = await db.Suppliers.SingleOrDefaultAsync(x => x.Id == supplierId && x.IsActive, cancellationToken)
            ?? throw new DomainException("El proveedor no existe o ya está inactivo.");
        if (supplier.Revision != expectedRevision)
            throw new ConcurrencyConflictException("El proveedor fue modificado por otra persona. Recarga la lista e inténtalo de nuevo.");
        supplier.Deactivate();
        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateConcurrencyException) { throw new ConcurrencyConflictException("El proveedor fue modificado por otra persona. Recarga la lista e inténtalo de nuevo."); }
        return supplier;
    }

    public async Task<IReadOnlyList<PurchaseOrder>> GetPurchaseOrdersAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        return await db.PurchaseOrders.AsNoTracking().AsSplitQuery().Include(x => x.Lines).Include(x => x.Payments).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<PayableBalanceDto>> GetOpenPayablesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var openOrders = await db.PurchaseOrders.AsNoTracking()
            .Where(x => x.Status != PurchaseStatus.Cancelled)
            .Select(x => new { x.Id, x.Number, x.SupplierName, x.Reference, x.DueDate, x.Total })
            .ToListAsync(cancellationToken);

        var amountsPaid = new Dictionary<Guid, decimal>();
        foreach (var idBatch in openOrders.Select(x => x.Id).Chunk(500))
        {
            var batchTotals = await db.SupplierPayments.AsNoTracking()
                .Where(x => idBatch.Contains(x.PurchaseOrderId))
                .GroupBy(x => x.PurchaseOrderId)
                .Select(group => new { PurchaseOrderId = group.Key, AmountPaid = group.Sum(x => x.Amount) })
                .ToListAsync(cancellationToken);
            foreach (var total in batchTotals) amountsPaid[total.PurchaseOrderId] = total.AmountPaid;
        }

        return openOrders.Select(order =>
            {
                var paid = amountsPaid.GetValueOrDefault(order.Id);
                return new PayableBalanceDto(order.Id, order.Number, order.SupplierName, order.Reference, order.DueDate, order.Total,
                    paid, decimal.Round(order.Total - paid, 2));
            })
            .Where(x => x.BalanceDue > 0)
            .OrderBy(x => x.DueDate)
            .ToArray();
    }

    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0) throw new DomainException("Agrega productos antes de guardar la compra.");
        if (request.Lines.Count > 200 || request.Lines.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0 || x.UnitCost < 0))
            throw new DomainException("La compra debe tener hasta 200 líneas con productos, cantidades y costos válidos.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var supplier = await db.Suppliers.FromSqlInterpolated($"SELECT * FROM Suppliers WHERE Id = {request.SupplierId} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("El proveedor no existe o está inactivo.");
        var lines = new List<PurchaseOrderLine>();
        foreach (var grouped in request.Lines.GroupBy(x => x.ProductId).OrderBy(x => x.Key))
        {
            var quantity = SumQuantities(grouped.Select(x => x.Quantity));
            if (quantity <= 0) throw new DomainException("La cantidad comprada debe ser mayor que cero.");
            var distinctCosts = grouped.Select(x => decimal.Round(x.UnitCost, 2)).Distinct().ToArray();
            if (distinctCosts.Length != 1) throw new DomainException("Las líneas repetidas del producto deben tener el mismo costo.");
            var product = await db.Products.FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {grouped.Key} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("Uno de los productos ya no está disponible.");
            lines.Add(new PurchaseOrderLine(product.Id, product.Sku, product.Name, quantity, distinctCosts[0]));
        }
        var number = $"C-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var order = new PurchaseOrder(tenantId, number, supplier, lines, request.Reference, request.DueDate);
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    public async Task<PurchaseOrder> ReceivePurchaseOrderAsync(Guid purchaseOrderId, IReadOnlyCollection<ReceivePurchaseLineRequest> lines, CancellationToken cancellationToken = default)
    {
        if (lines.Count == 0) throw new DomainException("Indica al menos una línea recibida.");
        if (lines.Count > 200 || lines.Any(x => x.LineId == Guid.Empty || x.Quantity <= 0))
            throw new DomainException("La recepción debe incluir hasta 200 líneas con cantidades válidas.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.PurchaseOrders
            .FromSqlInterpolated($"SELECT * FROM PurchaseOrders WHERE Id = {purchaseOrderId} AND TenantId = {tenantId} FOR UPDATE")
            .AsSplitQuery()
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(cancellationToken) ?? throw new DomainException("La orden de compra no existe.");
        foreach (var receipt in lines.GroupBy(x => x.LineId).Select(x => new ReceivePurchaseLineRequest(x.Key, SumQuantities(x.Select(y => y.Quantity)))))
        {
            var line = order.Receive(receipt.LineId, receipt.Quantity);
            var product = await db.Products.FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {line.ProductId} AND TenantId = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("El producto de la orden ya no existe.");
            product.AdjustStock(receipt.Quantity);
            db.InventoryMovements.Add(new InventoryMovement(tenantId, product.Id, InventoryMovementType.PurchaseReceipt, receipt.Quantity, product.StockOnHand, order.Number));
        }
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    public async Task<PurchaseOrder> CancelPurchaseOrderAsync(Guid purchaseOrderId, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.PurchaseOrders
            .FromSqlInterpolated($"SELECT * FROM PurchaseOrders WHERE Id = {purchaseOrderId} AND TenantId = {tenantId} FOR UPDATE")
            .AsSplitQuery().Include(x => x.Lines).Include(x => x.Payments)
            .SingleOrDefaultAsync(cancellationToken) ?? throw new DomainException("La orden de compra no existe.");
        order.Cancel();
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    public async Task<PurchaseOrder> RecordSupplierPaymentAsync(Guid purchaseOrderId, Guid paidByUserId, CreateSupplierPaymentRequest request, CancellationToken cancellationToken = default)
    {
        if (request.IdempotencyKey == Guid.Empty) throw new DomainException("La clave del pago es obligatoria.");
        if (paidByUserId == Guid.Empty) throw new DomainException("No se pudo identificar al usuario que registra el pago.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var order = await db.PurchaseOrders
            .FromSqlInterpolated($"SELECT * FROM PurchaseOrders WHERE Id = {purchaseOrderId} AND TenantId = {tenantId} FOR UPDATE")
            .Include(x => x.Lines)
            .Include(x => x.Payments)
            .SingleOrDefaultAsync(cancellationToken) ?? throw new DomainException("La orden de compra no existe.");

        var existingPayment = await db.SupplierPayments.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existingPayment is not null)
        {
            var sameRequest = existingPayment.PurchaseOrderId == order.Id
                && existingPayment.Amount == decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero)
                && existingPayment.Method == request.Method
                && existingPayment.Reference == (string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim());
            if (sameRequest) return order;
            throw new DomainException("La clave de idempotencia ya corresponde a otro pago.");
        }

        CashSession? cashSession = null;
        if (request.Method == SupplierPaymentMethod.Cash)
        {
            cashSession = await db.CashSessions.FromSqlInterpolated($"SELECT * FROM CashSessions WHERE TenantId = {tenantId} AND RegisterCode = {"MAIN"} AND Status = {(int)CashSessionStatus.Open} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("Para pagar en efectivo debe estar abierta la caja principal.");
        }

        var payment = new SupplierPayment(tenantId, order.Id, paidByUserId, request.IdempotencyKey, request.Amount,
            request.Method, request.Reference, cashSession?.Id);
        order.RecordPayment(payment);
        db.SupplierPayments.Add(payment);
        if (cashSession is not null)
            db.CashMovements.Add(new CashMovement(tenantId, cashSession.Id, paidByUserId, CashMovementKind.CashOut,
                payment.Amount, $"Pago a proveedor · {order.Number}"));

        try { await db.SaveChangesAsync(cancellationToken); }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "No se pudo persistir el pago de proveedor {PurchaseOrderId}.", purchaseOrderId);
            throw new DomainException("El pago no se pudo registrar; verifica si la clave ya se usó e inténtalo de nuevo.");
        }
        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    private async Task<StoreDbContext> CreateDbContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        return db;
    }

    private static int SumQuantities(IEnumerable<int> quantities)
    {
        try { return quantities.Aggregate(0, (sum, quantity) => checked(sum + quantity)); }
        catch (OverflowException) { throw new DomainException("La cantidad total supera el límite permitido."); }
    }
}
