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
using OJunnySyS.Domain.Pos;
using OJunnySyS.Domain.Inventory;
using OJunnySyS.Domain.Accounting;
using OJunnySyS.Infrastructure.Identity;

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

    public async Task<InventoryValuationDto> GetInventoryValuationAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var inventory = db.Products.AsNoTracking().Where(x => x.IsActive);
        var grossValue = await inventory.SumAsync(x => x.UnitCost * x.StockOnHand, cancellationToken);
        var uncostedValue = await inventory.SumAsync(x => x.UnitCost * x.UncostedQuantity, cancellationToken);
        var uncostedUnits = await inventory.SumAsync(x => x.UncostedQuantity, cancellationToken);
        return new InventoryValuationDto(Math.Max(0m, grossValue - uncostedValue), uncostedUnits);
    }

    public async Task<IReadOnlyList<ProductStockSnapshot>> SearchProductsInWarehouseAsync(string? query, Guid warehouseId, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive, cancellationToken))
            throw new DomainException("El almacén del terminal no existe o está inactivo.");
        var products = from stock in db.WarehouseStocks.AsNoTracking()
                       join product in db.Products.AsNoTracking() on new { stock.TenantId, stock.ProductId } equals new { product.TenantId, ProductId = product.Id }
                       where stock.WarehouseId == warehouseId && product.IsActive
                       select new { Product = product, stock.Quantity };
        if (!string.IsNullOrWhiteSpace(query))
            products = products.Where(x => x.Product.Name.Contains(query) || x.Product.Sku.Contains(query) || (x.Product.Barcode != null && x.Product.Barcode.Contains(query)));
        return await products.OrderBy(x => x.Product.Name).Take(100)
            .Select(x => new ProductStockSnapshot(x.Product, x.Quantity)).ToListAsync(cancellationToken);
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
            .Include(x => x.Payments).Include(x => x.Receipts).OrderByDescending(x => x.CreatedAtUtc).Take(Math.Clamp(count, 1, 200)).ToListAsync(cancellationToken);
    }

    public async Task<CustomerStatementData> GetCustomerStatementDataAsync(Guid customerId, DateTimeOffset startUtc, DateTimeOffset endUtc, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var sales = db.Sales.AsNoTracking().Where(x => x.CustomerId == customerId && x.Status == SaleStatus.Completed);
        var openingDebits = await sales.Where(x => x.CreatedAtUtc < startUtc).SumAsync(x => (decimal?)x.Total, cancellationToken) ?? 0m;
        var openingSalePayments = await (from payment in db.SalePayments.AsNoTracking()
                                         join sale in sales on new { payment.TenantId, SaleId = payment.SaleId } equals new { sale.TenantId, SaleId = sale.Id }
                                         where payment.CreatedAtUtc < startUtc
                                         select (decimal?)payment.Amount).SumAsync(cancellationToken) ?? 0m;
        var openingReceipts = await (from receipt in db.CustomerReceipts.AsNoTracking()
                                     join sale in sales on new { receipt.TenantId, SaleId = receipt.SaleId } equals new { sale.TenantId, SaleId = sale.Id }
                                     where receipt.CreatedAtUtc < startUtc
                                     select (decimal?)receipt.Amount).SumAsync(cancellationToken) ?? 0m;
        var opening = decimal.Round(openingDebits - openingSalePayments - openingReceipts, 2, MidpointRounding.AwayFromZero);

        var invoiceRows = await sales.Where(x => x.CreatedAtUtc >= startUtc && x.CreatedAtUtc < endUtc)
            .Select(x => new { x.Id, x.Number, x.CreatedAtUtc, x.DueDate, x.Total, x.PaymentMethod })
            .ToListAsync(cancellationToken);
        var salePaymentRows = await (from payment in db.SalePayments.AsNoTracking()
                                     join sale in sales on new { payment.TenantId, SaleId = payment.SaleId } equals new { sale.TenantId, SaleId = sale.Id }
                                     where payment.CreatedAtUtc >= startUtc && payment.CreatedAtUtc < endUtc
                                     select new { SaleId = sale.Id, sale.Number, payment.CreatedAtUtc, payment.Amount, payment.MethodName })
            .ToListAsync(cancellationToken);
        var receiptRows = await (from receipt in db.CustomerReceipts.AsNoTracking()
                                 join sale in sales on new { receipt.TenantId, SaleId = receipt.SaleId } equals new { sale.TenantId, SaleId = sale.Id }
                                 where receipt.CreatedAtUtc >= startUtc && receipt.CreatedAtUtc < endUtc
                                 select new { SaleId = sale.Id, sale.Number, receipt.CreatedAtUtc, receipt.Amount, receipt.MethodName, receipt.Reference })
            .ToListAsync(cancellationToken);
        var movements = new List<CustomerStatementMovement>(invoiceRows.Count + salePaymentRows.Count + receiptRows.Count);
        movements.AddRange(invoiceRows.Select(x => new CustomerStatementMovement(x.Id, x.CreatedAtUtc, x.Number,
            CustomerStatementEntryType.Sale, x.PaymentMethod == PaymentMethod.Credit ? "Venta a crédito" : "Venta", x.DueDate, x.Total, 0m)));
        movements.AddRange(salePaymentRows.Select(x => new CustomerStatementMovement(x.SaleId, x.CreatedAtUtc, x.Number,
            CustomerStatementEntryType.Payment, $"Cobro · {x.MethodName}", null, 0m, x.Amount)));
        movements.AddRange(receiptRows.Select(x => new CustomerStatementMovement(x.SaleId, x.CreatedAtUtc, x.Number,
            CustomerStatementEntryType.Payment, $"Abono · {x.MethodName}{(string.IsNullOrWhiteSpace(x.Reference) ? "" : $" · {x.Reference}")}", null, 0m, x.Amount)));
        return new CustomerStatementData(opening, movements);
    }

    public async Task<IReadOnlyList<Sale>> GetRecentSalesAsync(int count, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        return await db.Sales.AsNoTracking().Include(x => x.Payments).Include(x => x.Receipts).OrderByDescending(x => x.CreatedAtUtc).Take(Math.Clamp(count, 1, 100)).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Sale>> GetSalesInPeriodAsync(DateTimeOffset from, DateTimeOffset to, Guid? warehouseId = null, Guid? sellerUserId = null, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var query = db.Sales.AsNoTracking().Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Receipts)
            .Where(x => x.CreatedAtUtc >= from && x.CreatedAtUtc < to);
        if (warehouseId is { } warehouse) query = query.Where(x => x.WarehouseId == warehouse);
        if (sellerUserId is { } seller) query = query.Where(x => x.SellerUserId == seller);
        return await query.ToListAsync(cancellationToken);
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

    public async Task<Product> CreateProductAsync(string sku, string name, string category, decimal unitPrice, int stock, string? barcode, decimal? taxRateOverride = null, decimal? initialUnitCost = null, CancellationToken cancellationToken = default)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        var product = new Product(tenantId, sku, name, category, unitPrice, stock, barcode, taxRateOverride, initialUnitCost);
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La empresa activa no existe.");
        if (await db.Products.AnyAsync(x => x.Sku == product.Sku || (product.Barcode != null && x.Barcode == product.Barcode), cancellationToken))
            throw new DomainException("Ya existe un producto con ese SKU o código de barras.");
        var warehouses = await db.Warehouses.Where(x => x.IsActive).OrderBy(x => x.Code).ToListAsync(cancellationToken);
        var mainWarehouse = warehouses.SingleOrDefault(x => x.Code == "MAIN") ?? throw new DomainException("Configura el almacén principal antes de crear productos.");
        db.Products.Add(product);
        foreach (var warehouse in warehouses)
            db.WarehouseStocks.Add(new WarehouseStock(tenantId, warehouse.Id, product.Id, warehouse.Id == mainWarehouse.Id ? stock : 0));
        if (stock > 0) db.InventoryMovements.Add(new InventoryMovement(tenantId, product.Id, InventoryMovementType.StockAdjustment, stock, stock, "Existencias iniciales del producto", mainWarehouse.Id));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return product;
    }

    public async Task<Product> UpdateProductAsync(Guid productId, UpdateProductRequest request, CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        var product = await db.Products.SingleOrDefaultAsync(x => x.Id == productId, cancellationToken)
            ?? throw new DomainException("El producto no existe en esta empresa.");
        if (product.Revision != request.ExpectedRevision)
            throw new ConcurrencyConflictException("El producto cambió en otra sesión. Recarga el catálogo antes de guardar.");
        product.UpdateCatalog(request.Sku, request.Name, request.Category, request.UnitPrice, request.Barcode, request.TaxRateOverride);
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

    public async Task<InventoryTransferResult> TransferInventoryAsync(InventoryTransferRequest request, CancellationToken cancellationToken = default)
    {
        if (request.ProductId == Guid.Empty || request.SourceWarehouseId == Guid.Empty || request.DestinationWarehouseId == Guid.Empty || request.SourceWarehouseId == request.DestinationWarehouseId)
            throw new DomainException("Selecciona un producto y dos almacenes distintos.");
        if (request.Quantity <= 0) throw new DomainException("La cantidad a trasladar debe ser mayor que cero.");
        if (request.Reference?.Trim().Length > 100) throw new DomainException("La referencia del traslado no puede superar 100 caracteres.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La empresa activa no existe.");
        var product = await db.Products.FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {request.ProductId} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("El producto no existe o está inactivo.");
        var warehouseIds = new[] { request.SourceWarehouseId, request.DestinationWarehouseId }.Order().ToArray();
        foreach (var warehouseId in warehouseIds)
            if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive, cancellationToken))
                throw new DomainException("Los dos almacenes deben existir y estar activos en esta empresa.");
        var stocks = new Dictionary<Guid, WarehouseStock>();
        foreach (var warehouseId in warehouseIds)
            stocks[warehouseId] = await db.WarehouseStocks.FromSqlInterpolated($"SELECT * FROM WarehouseStocks WHERE TenantId = {tenantId} AND WarehouseId = {warehouseId} AND ProductId = {product.Id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("No existe el saldo del producto en uno de los almacenes.");
        var source = stocks[request.SourceWarehouseId];
        var destination = stocks[request.DestinationWarehouseId];
        source.Adjust(-request.Quantity);
        destination.Adjust(request.Quantity);
        var reference = string.IsNullOrWhiteSpace(request.Reference) ? $"TR-{Guid.NewGuid():N}" : request.Reference.Trim();
        db.InventoryMovements.Add(new InventoryMovement(tenantId, product.Id, InventoryMovementType.TransferOut, -request.Quantity, source.Quantity, reference, request.SourceWarehouseId));
        db.InventoryMovements.Add(new InventoryMovement(tenantId, product.Id, InventoryMovementType.TransferIn, request.Quantity, destination.Quantity, reference, request.DestinationWarehouseId));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new InventoryTransferResult(reference, request.Quantity, product.Id, request.SourceWarehouseId, source.Quantity, request.DestinationWarehouseId, destination.Quantity);
    }

    public async Task<Sale> CheckoutAsync(CheckoutRequest request, Guid cashierUserId, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0) throw new DomainException("Agrega productos antes de cobrar.");
        if (request.Lines.Count > 200 || request.Lines.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0)) throw new DomainException("La venta debe tener hasta 200 líneas con productos y cantidades válidas.");
        if (request.IdempotencyKey == Guid.Empty) throw new DomainException("La clave de idempotencia del cobro es obligatoria.");
        var registerCode = PosRegister.NormalizeCode(request.RegisterCode);
        if (request.Payments is null && request.PaymentMethod == PaymentMethod.Credit) throw new DomainException("Las ventas a crédito aún no están habilitadas.");
        if (cashierUserId == Guid.Empty) throw new DomainException("No se pudo identificar al cajero.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var priorSale = await db.Sales.AsNoTracking().Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Receipts)
            .SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (priorSale is not null)
        {
            EnsureSameCheckout(priorSale, request, cashierUserId, registerCode);
            await transaction.CommitAsync(cancellationToken);
            return priorSale;
        }
        var register = await db.PosRegisters.SingleOrDefaultAsync(x => x.Code == registerCode && x.IsActive, cancellationToken)
            ?? throw new DomainException("El terminal seleccionado no existe o está inactivo.");
        var warehouseId = register.WarehouseId;
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive, cancellationToken))
            throw new DomainException("El almacén asignado al terminal no existe o está inactivo.");
        CashSession? cashSession = null;
        if (request.PaymentMethod != PaymentMethod.Credit)
            cashSession = await db.CashSessions.FromSqlInterpolated($"SELECT * FROM CashSessions WHERE TenantId = {tenantId} AND RegisterCode = {registerCode} AND Status = {(int)CashSessionStatus.Open} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("Abre el turno del terminal seleccionado antes de registrar ventas.");
        priorSale = await LoadSaleByCheckoutKeyAsync(db, tenantId, request.IdempotencyKey, cancellationToken);
        if (priorSale is not null)
        {
            EnsureSameCheckout(priorSale, request, cashierUserId, registerCode);
            await transaction.CommitAsync(cancellationToken);
            return priorSale;
        }
        var customer = request.CustomerId is { } id
            ? await db.Customers.FromSqlInterpolated($"SELECT * FROM Customers WHERE Id = {id} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            : null;
        if (request.CustomerId is not null && customer is null) throw new DomainException("El cliente seleccionado no existe o está inactivo.");
        var sellerName = await ResolveSellerNameAsync(db, tenantId, request.SellerUserId, cancellationToken);

        var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new DomainException("No se encontró la empresa activa.");
        var lines = new List<SaleLine>();
        var stockMovements = new List<(Product Product, int Quantity, int WarehouseBalance)>();
        foreach (var groupedLine in request.Lines.GroupBy(x => x.ProductId).OrderBy(x => x.Key))
        {
            var quantity = SumQuantities(groupedLine.Select(x => x.Quantity));
            if (quantity <= 0) throw new DomainException("La cantidad debe ser mayor que cero.");
            var product = await db.Products.FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {groupedLine.Key} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("Uno de los productos ya no está disponible.");
            var warehouseStock = await db.WarehouseStocks.FromSqlInterpolated($"SELECT * FROM WarehouseStocks WHERE TenantId = {tenantId} AND WarehouseId = {warehouseId} AND ProductId = {product.Id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("No hay un saldo inicializado para este producto en el almacén del terminal.");
            warehouseStock.Adjust(-quantity);
            var costSnapshot = product.ConsumeInventory(quantity);
            stockMovements.Add((product, quantity, warehouseStock.Quantity));
            lines.Add(new SaleLine(product.Id, product.Sku, product.Name, product.UnitPrice, quantity,
                product.TaxRateOverride ?? tenant.DefaultTaxRate, costSnapshot.UnitCost, costSnapshot.UncostedQuantity));
        }

        if (request.PaymentMethod == PaymentMethod.Credit && (customer is null || request.DueDate is null)) throw new DomainException("Selecciona un cliente y una fecha de vencimiento para vender a crédito.");
        var sale = new Sale(tenantId, $"V-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}", customer, request.PaymentMethod, lines, cashSession?.Id, cashierUserId, tenant.DefaultTaxRate, request.IdempotencyKey, registerCode, warehouseId, request.DueDate);
        sale.AssignSeller(request.SellerUserId, sellerName);
        sale.RecordPayments(request.PaymentMethod == PaymentMethod.Credit ? [] : await ResolvePaymentAllocationsAsync(db, tenantId, request, sale.Total, cancellationToken));
        foreach (var movement in stockMovements)
            db.InventoryMovements.Add(new InventoryMovement(tenantId, movement.Product.Id, InventoryMovementType.Sale, -movement.Quantity, movement.WarehouseBalance, sale.Number, warehouseId));
        customer?.RecordPurchase(sale.Total);
        db.Sales.Add(sale);
        await PostSaleJournalEntryAsync(db, tenantId, sale, cashierUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return sale;
    }

    private static async Task PostSaleJournalEntryAsync(StoreDbContext db, Guid tenantId, Sale sale, Guid actorId, CancellationToken ct)
    {
        if (sale.Total <= 0) return;

        var paymentAccountIds = sale.Payments.Select(x => x.LedgerAccountId).Distinct().ToArray();
        var codes = sale.PaymentMethod == PaymentMethod.Credit
            ? sale.TaxAmount > 0 ? new[] { "1200", "1100", "2100", "4000", "5000" } : new[] { "1200", "1100", "4000", "5000" }
            : sale.TaxAmount > 0 ? new[] { "1200", "2100", "4000", "5000" } : new[] { "1200", "4000", "5000" };
        var accounts = await db.ChartAccounts.Where(x => (codes.Contains(x.Code) || paymentAccountIds.Contains(x.Id)) && x.IsActive)
            .ToListAsync(ct);
        var accountsByCode = accounts.ToDictionary(x => x.Code);
        var accountsById = accounts.ToDictionary(x => x.Id);
        if (codes.Any(code => !accountsByCode.ContainsKey(code)) || paymentAccountIds.Any(id => !accountsById.ContainsKey(id)))
            throw new DomainException("Faltan las cuentas contables base para registrar la venta. Revisa el catálogo contable.");
        if (paymentAccountIds.Any(id => !accountsById[id].CanReceivePayments))
            throw new DomainException("Los medios de pago deben usar cuentas de caja, banco o saldo de pago.");

        var lines = sale.PaymentMethod == PaymentMethod.Credit
            ? new List<JournalEntryLine> { new(tenantId, accountsByCode["1100"].Id, accountsByCode["1100"].Code,
                accountsByCode["1100"].Name, sale.Total, 0, $"Cuenta por cobrar · {sale.CustomerName}") }
            : sale.Payments.GroupBy(x => x.LedgerAccountId).Select(group =>
        {
            var account = accountsById[group.Key];
            return new JournalEntryLine(tenantId, account.Id, account.Code, account.Name,
                group.Sum(x => x.Amount), 0, $"Cobro de venta {sale.Number} · {string.Join(", ", group.Select(x => x.MethodName).Distinct())}");
        }).ToList();
        if (sale.Subtotal > 0)
            lines.Add(new JournalEntryLine(tenantId, accountsByCode["4000"].Id, accountsByCode["4000"].Code,
                accountsByCode["4000"].Name, 0, sale.Subtotal, $"Venta {sale.Number}"));
        if (sale.TaxAmount > 0)
            lines.Add(new JournalEntryLine(tenantId, accountsByCode["2100"].Id, accountsByCode["2100"].Code,
                accountsByCode["2100"].Name, 0, sale.TaxAmount, $"Impuesto de venta {sale.Number}"));
        var costOfGoods = sale.Lines.Sum(x => x.CostTotal);
        if (costOfGoods > 0)
        {
            lines.Add(new JournalEntryLine(tenantId, accountsByCode["5000"].Id, accountsByCode["5000"].Code,
                accountsByCode["5000"].Name, costOfGoods, 0, $"Costo de productos vendidos {sale.Number}"));
            lines.Add(new JournalEntryLine(tenantId, accountsByCode["1200"].Id, accountsByCode["1200"].Code,
                accountsByCode["1200"].Name, 0, costOfGoods, $"Salida de inventario {sale.Number}"));
        }

        var entryDate = DateOnly.FromDateTime(sale.CreatedAtUtc.UtcDateTime);
        var number = $"AUTO-{entryDate:yyyyMMdd}-{sale.Id:N}"[..36];
        var entry = new JournalEntry(tenantId, number, entryDate,
            $"Venta POS {sale.Number}", actorId, lines, "SALE", sale.Id);
        db.JournalEntries.Add(entry);
    }

    private static async Task<Sale?> LoadSaleByCheckoutKeyAsync(StoreDbContext db, Guid tenantId, Guid idempotencyKey, CancellationToken ct) =>
        await db.Sales.FromSqlInterpolated($"SELECT * FROM Sales WHERE TenantId = {tenantId} AND IdempotencyKey = {idempotencyKey} FOR UPDATE")
            .AsSplitQuery().Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Receipts).SingleOrDefaultAsync(ct);

    private static async Task<IReadOnlyList<PaymentAllocation>> ResolvePaymentAllocationsAsync(StoreDbContext db, Guid tenantId,
        CheckoutRequest request, decimal total, CancellationToken ct)
    {
        if (request.Payments is null)
        {
            var kind = request.PaymentMethod switch
            {
                PaymentMethod.Cash => PaymentMethodType.Cash,
                PaymentMethod.Card => PaymentMethodType.Card,
                PaymentMethod.Transfer => PaymentMethodType.Transfer,
                PaymentMethod.Other => PaymentMethodType.Other,
                _ => throw new DomainException("El método de pago no es válido.")
            };
            var defaultCode = kind switch { PaymentMethodType.Cash => "cash", PaymentMethodType.Card => "card", PaymentMethodType.Transfer => "transfer", _ => "other" };
            var method = await db.TenantPaymentMethods.AsNoTracking().Where(x => x.IsActive && x.Type == kind)
                .OrderBy(x => x.Code == defaultCode ? 0 : 1).ThenBy(x => x.Name).FirstOrDefaultAsync(ct)
                ?? throw new DomainException("Ese medio de pago no está activo. Actualiza el POS y elige otra forma de pago.");
            if (method.LedgerAccountId is not { } ledgerAccountId) throw new DomainException("Configura una cuenta contable activa para este medio de pago.");
            return [new PaymentAllocation(method.Id, method.Name, method.Type, total, ledgerAccountId)];
        }

        if (request.Payments.Count is < 1 or > 10) throw new DomainException("Selecciona entre uno y diez medios de pago.");
        if (request.Payments.Any(x => x.PaymentMethodId == Guid.Empty) || request.Payments.Select(x => x.PaymentMethodId).Distinct().Count() != request.Payments.Count)
            throw new DomainException("Cada medio de pago puede aparecer una sola vez.");
        var methods = new Dictionary<Guid, TenantPaymentMethod>();
        foreach (var payment in request.Payments.OrderBy(x => x.PaymentMethodId))
        {
            var method = await db.TenantPaymentMethods.AsNoTracking().SingleOrDefaultAsync(x => x.Id == payment.PaymentMethodId && x.IsActive, ct)
                ?? throw new DomainException("Uno de los medios de pago ya no está disponible. Actualiza el POS.");
            if (method.LedgerAccountId is not { } ledgerAccountId || !await db.ChartAccounts.AnyAsync(x => x.Id == ledgerAccountId && x.IsActive && x.Type == AccountType.Asset && x.Code.StartsWith("10"), ct))
                throw new DomainException("Configura una cuenta activa de caja, banco o saldo de pago para cada medio antes de cobrar.");
            methods.Add(method.Id, method);
        }
        return request.Payments.Select(payment =>
        {
            var method = methods[payment.PaymentMethodId];
            return new PaymentAllocation(method.Id, method.Name, method.Type, payment.Amount, method.LedgerAccountId!.Value, payment.Reference);
        }).ToArray();
    }

    private static async Task<string?> ResolveSellerNameAsync(StoreDbContext db, Guid tenantId, Guid? sellerUserId, CancellationToken ct)
    {
        if (sellerUserId is null) return null;
        var seller = await db.Users.FromSqlInterpolated($"SELECT * FROM AspNetUsers WHERE Id = {sellerUserId} AND TenantId = {tenantId} AND (LockoutEnd IS NULL OR LockoutEnd <= UTC_TIMESTAMP()) FOR UPDATE")
            .SingleOrDefaultAsync(ct) ?? throw new DomainException("El vendedor seleccionado no está activo en esta empresa.");
        var allowedRoles = new[] { AppRoles.Administrator, AppRoles.Manager, AppRoles.Salesperson };
        var hasSellerRole = await (from userRole in db.UserRoles
                                   join role in db.Roles on userRole.RoleId equals role.Id
                                   where userRole.UserId == seller.Id && role.Name != null && allowedRoles.Contains(role.Name)
                                   select role.Id).AnyAsync(ct);
        if (!hasSellerRole) throw new DomainException("La cuenta seleccionada no puede asignarse como vendedor.");
        return seller.DisplayName;
    }

    private static void EnsureSameCheckout(Sale sale, CheckoutRequest request, Guid cashierUserId, string registerCode)
    {
        var requestedLines = request.Lines.GroupBy(x => x.ProductId).ToDictionary(x => x.Key, x => SumQuantities(x.Select(line => line.Quantity)));
        var storedLines = sale.Lines.ToDictionary(x => x.ProductId, x => x.Quantity);
        if (sale.CashierUserId != cashierUserId || sale.CustomerId != request.CustomerId || sale.SellerUserId != request.SellerUserId || sale.PaymentMethod != request.PaymentMethod
            || (request.PaymentMethod == PaymentMethod.Credit && sale.DueDate != request.DueDate) || sale.RegisterCode != registerCode
            || requestedLines.Count != storedLines.Count || requestedLines.Any(x => !storedLines.TryGetValue(x.Key, out var quantity) || quantity != x.Value))
            throw new DomainException("La clave de idempotencia ya se utilizó con otros datos de cobro.");
        if (request.Payments is not null)
        {
            var expected = request.Payments.ToDictionary(x => x.PaymentMethodId, x => (x.Amount, Reference: x.Reference?.Trim()));
            var actual = sale.Payments.ToDictionary(x => x.PaymentMethodId, x => (x.Amount, x.Reference));
            if (expected.Count != actual.Count || expected.Any(x => !actual.TryGetValue(x.Key, out var value) || value != x.Value))
                throw new DomainException("La clave de idempotencia ya se utilizó con otra distribución de pagos.");
        }
    }

    public async Task<CashSessionDto?> GetCurrentCashSessionAsync(string registerCode = "MAIN", CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        registerCode = PosRegister.NormalizeCode(registerCode);
        var session = await db.CashSessions.AsNoTracking()
            .Where(x => x.RegisterCode == registerCode && x.Status == CashSessionStatus.Open)
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
        var registerCode = PosRegister.NormalizeCode(request.RegisterCode);
        if (!await db.PosRegisters.AnyAsync(x => x.Code == registerCode && x.IsActive, cancellationToken))
            throw new DomainException("El terminal seleccionado no existe o está inactivo.");
        if (await db.CashSessions.AnyAsync(x => x.RegisterCode == registerCode && x.Status == CashSessionStatus.Open, cancellationToken))
            throw new DomainException("Este terminal ya tiene un turno abierto.");
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
        var sales = await GetCashTenderSalesAsync(db, session.Id, cancellationToken);
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
        var sale = await db.Sales.AsNoTracking().Include(x => x.Lines).Include(x => x.Payments).SingleOrDefaultAsync(x => x.Id == saleId, cancellationToken);
        if (sale is null) return null;
        var registerCode = sale.CashSessionId is { } sessionId
            ? await db.CashSessions.AsNoTracking().Where(x => x.Id == sessionId).Select(x => x.RegisterCode).SingleOrDefaultAsync(cancellationToken)
            : null;
        var receiptLines = sale.Lines.Select(x => new ReceiptLineDto(x.Sku, x.ProductName, x.Quantity, x.UnitPrice, x.LineTotal, x.TaxRate, x.TaxAmount)).ToArray();
        var unallocatedTax = sale.TaxAmount - receiptLines.Sum(x => x.TaxAmount);
        if (receiptLines.Length > 0 && unallocatedTax != 0)
            receiptLines[^1] = receiptLines[^1] with { TaxAmount = receiptLines[^1].TaxAmount + unallocatedTax };
        return new SaleReceiptDto(sale.Id, sale.Number, sale.CustomerName, sale.Subtotal, sale.TaxRate, sale.TaxAmount, sale.Total,
            sale.PaymentMethod, sale.Status, sale.CreatedAtUtc, registerCode,
            receiptLines,
            sale.Payments.Select(x => new SalePaymentDto(x.MethodName, x.Type, x.Amount, x.Reference)).ToArray(), sale.SellerName);
    }

    private static async Task<CashSessionDto> MapCashSessionAsync(StoreDbContext db, CashSession session, CancellationToken cancellationToken)
    {
        var sales = await GetCashTenderSalesAsync(db, session.Id, cancellationToken);
        var cashIn = await db.CashMovements.AsNoTracking().Where(x => x.SessionId == session.Id && x.Kind == CashMovementKind.CashIn).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var cashOut = await db.CashMovements.AsNoTracking().Where(x => x.SessionId == session.Id && x.Kind == CashMovementKind.CashOut).SumAsync(x => (decimal?)x.Amount, cancellationToken) ?? 0m;
        var movements = await db.CashMovements.AsNoTracking().Where(x => x.SessionId == session.Id).OrderByDescending(x => x.CreatedAtUtc).Take(100).ToListAsync(cancellationToken);
        var expected = session.ExpectedCash ?? decimal.Round(session.OpeningFloat + sales + cashIn - cashOut, 2);
        return new CashSessionDto(session.Id, session.RegisterCode, session.Status, session.OpeningFloat, session.OpenedByUserId, session.OpenedAtUtc,
            sales, cashIn, cashOut, expected, session.ClosedAtUtc, session.CountedCash, session.Difference,
            movements.Select(x => new CashMovementDto(x.Id, x.Kind, x.Amount, x.Description, x.UserId, x.CreatedAtUtc)).ToArray());
    }

    private static async Task<decimal> GetCashTenderSalesAsync(StoreDbContext db, Guid sessionId, CancellationToken ct) =>
        await (from payment in db.SalePayments.AsNoTracking()
         join sale in db.Sales.AsNoTracking() on new { payment.TenantId, payment.SaleId } equals new { sale.TenantId, SaleId = sale.Id }
         where sale.CashSessionId == sessionId && sale.Status == SaleStatus.Completed && payment.Type == PaymentMethodType.Cash
         select (decimal?)payment.Amount).SumAsync(ct) ?? 0m;

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

    public async Task<IReadOnlyList<Sale>> GetOpenReceivablesAsync(CancellationToken cancellationToken = default)
    {
        await using var db = await CreateDbContextAsync(cancellationToken);
        return await db.Sales.AsNoTracking().Where(x => x.PaymentMethod == PaymentMethod.Credit && x.Status == SaleStatus.Completed)
            .Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Receipts)
            .OrderBy(x => x.DueDate).ThenByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(cancellationToken);
    }

    public async Task<Sale> RecordCustomerReceiptAsync(Guid saleId, Guid actorId, CustomerReceiptRequest request, CancellationToken cancellationToken = default)
    {
        if (request.IdempotencyKey == Guid.Empty || actorId == Guid.Empty) throw new DomainException("La clave del recibo y el usuario son obligatorios.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var idempotent = await db.CustomerReceipts.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (idempotent is not null)
        {
            if (idempotent.SaleId != saleId || idempotent.Amount != decimal.Round(request.Amount, 2) || idempotent.PaymentMethodId != request.PaymentMethodId || idempotent.Reference != (string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim()))
                throw new DomainException("La clave de idempotencia ya pertenece a otro recibo.");
            var existingSale = await db.Sales.AsNoTracking().Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Receipts).SingleAsync(x => x.Id == saleId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return existingSale;
        }
        var sale = await db.Sales.FromSqlInterpolated($"SELECT * FROM Sales WHERE TenantId = {tenantId} AND Id = {saleId} FOR UPDATE")
            .AsSplitQuery().Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Receipts).SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("La cuenta por cobrar no existe.");
        var racedReceipt = await db.CustomerReceipts.FromSqlInterpolated($"SELECT * FROM CustomerReceipts WHERE TenantId = {tenantId} AND IdempotencyKey = {request.IdempotencyKey} FOR UPDATE").AsNoTracking().SingleOrDefaultAsync(cancellationToken);
        if (racedReceipt is not null)
        {
            if (racedReceipt.SaleId != saleId || racedReceipt.Amount != decimal.Round(request.Amount, 2) || racedReceipt.PaymentMethodId != request.PaymentMethodId || racedReceipt.Reference != (string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim()))
                throw new DomainException("La clave de idempotencia ya pertenece a otro recibo.");
            await transaction.CommitAsync(cancellationToken);
            return sale;
        }
        if (sale.PaymentMethod != PaymentMethod.Credit || sale.CustomerId is null) throw new DomainException("La venta no tiene saldo a crédito.");
        var method = await db.TenantPaymentMethods.SingleOrDefaultAsync(x => x.Id == request.PaymentMethodId && x.IsActive, cancellationToken)
            ?? throw new DomainException("El medio de pago no existe o está inactivo.");
        if (method.LedgerAccountId is null) throw new DomainException("Configura la cuenta contable del medio de pago antes de cobrar.");
        CashSession? cashSession = null;
        var registerCode = PosRegister.NormalizeCode(request.RegisterCode);
        if (method.Type == PaymentMethodType.Cash)
            cashSession = await db.CashSessions.FromSqlInterpolated($"SELECT * FROM CashSessions WHERE TenantId = {tenantId} AND RegisterCode = {registerCode} AND Status = {(int)CashSessionStatus.Open} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("Abre el turno de caja para registrar un cobro en efectivo.");
        var receipt = new CustomerReceipt(tenantId, sale.Id, sale.CustomerId.Value, actorId, request.IdempotencyKey, method.Id, request.Amount,
            method.Name, method.Type, request.Reference, method.LedgerAccountId.Value, cashSession?.Id);
        sale.RecordReceivablePayment(receipt);
        db.CustomerReceipts.Add(receipt);
        var accounts = await db.ChartAccounts.Where(x => (x.Id == method.LedgerAccountId || x.Code == "1100") && x.IsActive).ToListAsync(cancellationToken);
        var paymentAccount = accounts.SingleOrDefault(x => x.Id == method.LedgerAccountId && x.CanReceivePayments);
        var receivableAccount = accounts.SingleOrDefault(x => x.Code == "1100");
        if (paymentAccount is null || receivableAccount is null) throw new DomainException("Revisa las cuentas activas de cobro y cuentas por cobrar.");
        var entry = new JournalEntry(tenantId, $"COB-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40],
            DateOnly.FromDateTime(DateTime.UtcNow), $"Cobro de cliente · {sale.Number}", actorId,
            [new JournalEntryLine(tenantId, paymentAccount.Id, paymentAccount.Code, paymentAccount.Name, receipt.Amount, 0, $"{method.Name} · {sale.CustomerName}"),
             new JournalEntryLine(tenantId, receivableAccount.Id, receivableAccount.Code, receivableAccount.Name, 0, receipt.Amount, $"Abono a {sale.Number}")],
            "CUSTOMERRECEIPT", receipt.Id);
        db.JournalEntries.Add(entry);
        if (cashSession is not null) db.CashMovements.Add(new CashMovement(tenantId, cashSession.Id, actorId, CashMovementKind.CashIn, receipt.Amount, $"Cobro a cliente · {sale.Number}"));
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return sale;
    }

    public async Task<PurchaseOrder> CreatePurchaseOrderAsync(CreatePurchaseOrderRequest request, CancellationToken cancellationToken = default)
    {
        if (request.Lines.Count == 0) throw new DomainException("Agrega productos antes de guardar la compra.");
        if (request.Lines.Count > 200 || request.Lines.Any(x => x.ProductId == Guid.Empty || x.Quantity <= 0 || x.UnitCost < 0 || x.TaxRate is < 0 or > 1))
            throw new DomainException("La compra debe tener hasta 200 líneas con productos, cantidades, costos e impuestos válidos.");
        var tenantId = tenantContext.GetRequiredTenantId();
        await using var db = await CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);
        var supplier = await db.Suppliers.FromSqlInterpolated($"SELECT * FROM Suppliers WHERE Id = {request.SupplierId} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
            ?? throw new DomainException("El proveedor no existe o está inactivo.");
        var warehouseId = request.WarehouseId;
        if (warehouseId == Guid.Empty)
            warehouseId = await db.Warehouses.Where(x => x.Code == "MAIN" && x.IsActive).Select(x => x.Id).SingleOrDefaultAsync(cancellationToken);
        if (!await db.Warehouses.AnyAsync(x => x.Id == warehouseId && x.IsActive, cancellationToken))
            throw new DomainException("Selecciona un almacén activo para recibir la compra.");
        var lines = new List<PurchaseOrderLine>();
        foreach (var grouped in request.Lines.GroupBy(x => new { x.ProductId, x.TaxRate }).OrderBy(x => x.Key.ProductId).ThenBy(x => x.Key.TaxRate))
        {
            var quantity = SumQuantities(grouped.Select(x => x.Quantity));
            if (quantity <= 0) throw new DomainException("La cantidad comprada debe ser mayor que cero.");
            var distinctCosts = grouped.Select(x => decimal.Round(x.UnitCost, 2)).Distinct().ToArray();
            if (distinctCosts.Length != 1) throw new DomainException("Las líneas repetidas del producto deben tener el mismo costo.");
            var product = await db.Products.FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {grouped.Key.ProductId} AND TenantId = {tenantId} AND IsActive = 1 FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("Uno de los productos ya no está disponible.");
            lines.Add(new PurchaseOrderLine(product.Id, product.Sku, product.Name, quantity, distinctCosts[0], grouped.Key.TaxRate));
        }
        var number = $"C-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}";
        var order = new PurchaseOrder(tenantId, number, supplier, lines, request.Reference, request.DueDate, warehouseId);
        db.PurchaseOrders.Add(order);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    public async Task<PurchaseOrder> ReceivePurchaseOrderAsync(Guid purchaseOrderId, Guid receivedByUserId, IReadOnlyCollection<ReceivePurchaseLineRequest> lines, CancellationToken cancellationToken = default)
    {
        if (receivedByUserId == Guid.Empty) throw new DomainException("No se pudo identificar al usuario que registra la recepción.");
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
        var receivedLines = new List<(PurchaseOrderLine Line, int Quantity, decimal Tax)>();
        foreach (var receipt in lines.GroupBy(x => x.LineId).Select(x => new ReceivePurchaseLineRequest(x.Key, SumQuantities(x.Select(y => y.Quantity)))))
        {
            var line = order.Lines.SingleOrDefault(x => x.Id == receipt.LineId) ?? throw new DomainException("La línea no pertenece a esta compra.");
            var previouslyReceived = line.ReceivedQuantity;
            order.Receive(receipt.LineId, receipt.Quantity);
            var taxBefore = decimal.Round(line.UnitCost * previouslyReceived * line.TaxRate, 2, MidpointRounding.AwayFromZero);
            var taxAfter = decimal.Round(line.UnitCost * line.ReceivedQuantity * line.TaxRate, 2, MidpointRounding.AwayFromZero);
            receivedLines.Add((line, receipt.Quantity, taxAfter - taxBefore));
            var product = await db.Products.FromSqlInterpolated($"SELECT * FROM Products WHERE Id = {line.ProductId} AND TenantId = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("El producto de la orden ya no existe.");
            var warehouseStock = await db.WarehouseStocks.FromSqlInterpolated($"SELECT * FROM WarehouseStocks WHERE TenantId = {tenantId} AND WarehouseId = {order.WarehouseId} AND ProductId = {product.Id} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("No existe el saldo del producto en el almacén de recepción.");
            product.ApplyPurchaseCost(receipt.Quantity, line.UnitCost);
            warehouseStock.Adjust(receipt.Quantity);
            product.AdjustStock(receipt.Quantity);
            db.InventoryMovements.Add(new InventoryMovement(tenantId, product.Id, InventoryMovementType.PurchaseReceipt, receipt.Quantity, warehouseStock.Quantity, order.Number, order.WarehouseId));
        }
        await PostPurchaseReceiptJournalEntryAsync(db, tenantId, order, receivedLines, receivedByUserId, cancellationToken);
        await db.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return order;
    }

    private static async Task PostPurchaseReceiptJournalEntryAsync(StoreDbContext db, Guid tenantId, PurchaseOrder order,
        IReadOnlyCollection<(PurchaseOrderLine Line, int Quantity, decimal Tax)> receivedLines, Guid actorId, CancellationToken ct)
    {
        var subtotal = receivedLines.Sum(x => decimal.Round(x.Line.UnitCost * x.Quantity, 2, MidpointRounding.AwayFromZero));
        var tax = receivedLines.Sum(x => x.Tax);
        var total = subtotal + tax;
        if (total <= 0) return;
        var codes = tax > 0 ? new[] { "1200", "1300", "2000" } : new[] { "1200", "2000" };
        var accounts = await db.ChartAccounts.Where(x => codes.Contains(x.Code) && x.IsActive).ToDictionaryAsync(x => x.Code, ct);
        if (codes.Any(code => !accounts.ContainsKey(code)))
            throw new DomainException("Faltan las cuentas de inventario, impuesto acreditable o cuentas por pagar para contabilizar la recepción.");
        var lines = new List<JournalEntryLine>
        {
            new(tenantId, accounts["1200"].Id, accounts["1200"].Code, accounts["1200"].Name, subtotal, 0, $"Inventario recibido · {order.Number}")
        };
        if (tax > 0) lines.Add(new JournalEntryLine(tenantId, accounts["1300"].Id, accounts["1300"].Code, accounts["1300"].Name, tax, 0, $"Impuesto acreditable · {order.Number}"));
        lines.Add(new JournalEntryLine(tenantId, accounts["2000"].Id, accounts["2000"].Code, accounts["2000"].Name, 0, total, $"Cuenta por pagar · {order.SupplierName}"));
        var batchId = Guid.NewGuid();
        var entry = new JournalEntry(tenantId, $"REC-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40],
            DateOnly.FromDateTime(DateTime.UtcNow), $"Recepción de compra {order.Number}", actorId, lines, "PURCHASERECEIPT", batchId);
        db.JournalEntries.Add(entry);
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
        var registerCode = PosRegister.NormalizeCode(request.RegisterCode);

        var existingPayment = await db.SupplierPayments.AsNoTracking().SingleOrDefaultAsync(x => x.IdempotencyKey == request.IdempotencyKey, cancellationToken);
        if (existingPayment is not null)
        {
            var sameRequest = existingPayment.PurchaseOrderId == order.Id
                && existingPayment.Amount == decimal.Round(request.Amount, 2, MidpointRounding.AwayFromZero)
                && existingPayment.Method == request.Method
                && existingPayment.RegisterCode == registerCode
                && existingPayment.Reference == (string.IsNullOrWhiteSpace(request.Reference) ? null : request.Reference.Trim());
            if (sameRequest) return order;
            throw new DomainException("La clave de idempotencia ya corresponde a otro pago.");
        }

        CashSession? cashSession = null;
        if (request.Method == SupplierPaymentMethod.Cash)
        {
            if (!await db.PosRegisters.AnyAsync(x => x.Code == registerCode && x.IsActive, cancellationToken))
                throw new DomainException("El terminal seleccionado no existe o está inactivo.");
            cashSession = await db.CashSessions.FromSqlInterpolated($"SELECT * FROM CashSessions WHERE TenantId = {tenantId} AND RegisterCode = {registerCode} AND Status = {(int)CashSessionStatus.Open} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("Para pagar en efectivo debe estar abierto el turno del terminal seleccionado.");
        }

        var ledgerCode = request.Method switch
        {
            SupplierPaymentMethod.Cash => "1000",
            SupplierPaymentMethod.BankTransfer or SupplierPaymentMethod.Check => "1010",
            SupplierPaymentMethod.Card => "1020",
            _ => "1030"
        };
        var paymentAccount = await db.ChartAccounts.SingleOrDefaultAsync(x => x.Code == ledgerCode && x.IsActive, cancellationToken);
        if (paymentAccount is null || !paymentAccount.CanReceivePayments)
            throw new DomainException("Activa una cuenta de caja, banco o saldo de pago para registrar abonos a proveedores.");
        var payment = new SupplierPayment(tenantId, order.Id, paidByUserId, request.IdempotencyKey, request.Amount,
            request.Method, request.Reference, cashSession?.Id, paymentAccount.Id, registerCode);
        order.RecordPayment(payment);
        db.SupplierPayments.Add(payment);
        var payableAccount = await db.ChartAccounts.SingleOrDefaultAsync(x => x.Code == "2000" && x.IsActive, cancellationToken)
            ?? throw new DomainException("La cuenta de cuentas por pagar está inactiva o no existe.");
        var paymentEntry = new JournalEntry(tenantId, $"PAG-{DateTimeOffset.UtcNow:yyyyMMddHHmmss}-{Guid.NewGuid():N}"[..40],
            DateOnly.FromDateTime(DateTime.UtcNow), $"Pago a proveedor {order.Number}", paidByUserId,
            [new JournalEntryLine(tenantId, payableAccount.Id, payableAccount.Code, payableAccount.Name, payment.Amount, 0, $"Abono · {order.SupplierName}"),
             new JournalEntryLine(tenantId, paymentAccount.Id, paymentAccount.Code, paymentAccount.Name, 0, payment.Amount, $"Pago {request.Method} · {order.Number}")],
            "SUPPLIERPAYMENT", payment.Id);
        db.JournalEntries.Add(paymentEntry);
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
