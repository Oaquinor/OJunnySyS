using Microsoft.EntityFrameworkCore;
using OJunnySyS.Application.Abstractions;
using OJunnySyS.Application.Contracts;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Sales;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class EfSalesQuoteRepository(
    IDbContextFactory<StoreDbContext> contextFactory,
    ITenantContext tenantContext,
    ITenantAccessGuard tenantAccess,
    IStoreRepository storeRepository) : ISalesQuoteRepository
{
    public async Task<IReadOnlyList<SalesQuote>> GetAsync(CancellationToken cancellationToken = default)
    {
        var db = await OpenTenantContextAsync(cancellationToken);
        await using (db)
            return await db.SalesQuotes.AsNoTracking().AsSplitQuery().Include(x => x.Lines)
                .OrderByDescending(x => x.CreatedAtUtc).Take(500).ToListAsync(cancellationToken);
    }

    public async Task<SalesQuote> CreateAsync(CreateSalesQuoteRequest request, DateOnly today, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.CustomerId == Guid.Empty) throw new DomainException("Selecciona un cliente.");
        if (request.ValidUntil < today) throw new DomainException("La vigencia debe ser hoy o una fecha futura.");
        var tenantId = tenantContext.GetRequiredTenantId();
        var db = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        await using (var transaction = await db.Database.BeginTransactionAsync(cancellationToken))
        {
            _ = await db.Tenants.FromSqlInterpolated($"SELECT * FROM Tenants WHERE Id = {tenantId} FOR UPDATE").SingleOrDefaultAsync(cancellationToken)
                ?? throw new DomainException("La empresa activa no existe.");
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken)
                ?? throw new DomainException("El cliente no existe o está inactivo en esta empresa.");
            var number = await NextNumberAsync(db, tenantId, today, cancellationToken);
            var quote = new SalesQuote(tenantId, number, customer, request.ValidUntil, request.Notes,
                await BuildLinesAsync(db, tenantId, request.Lines, cancellationToken));
            db.SalesQuotes.Add(quote);
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return quote;
        }
    }

    public async Task<SalesQuote> UpdateAsync(Guid id, UpdateSalesQuoteRequest request, DateOnly today, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        if (request.ValidUntil < today) throw new DomainException("La vigencia debe ser hoy o una fecha futura.");
        var db = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        {
            var quote = await db.SalesQuotes.AsSplitQuery().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new DomainException("La cotización no existe en esta empresa.");
            var customer = await db.Customers.SingleOrDefaultAsync(x => x.Id == request.CustomerId && x.IsActive, cancellationToken)
                ?? throw new DomainException("El cliente no existe o está inactivo en esta empresa.");
            quote.UpdateDraft(customer, request.ValidUntil, request.Notes,
                await BuildLinesAsync(db, quote.TenantId, request.Lines, cancellationToken), request.ExpectedRevision);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { throw Concurrency(); }
            return quote;
        }
    }

    public Task<SalesQuote> MarkSentAsync(Guid id, SalesQuoteStatusRequest request, DateOnly today, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, request, (quote, _) => quote.MarkSent(request.ExpectedRevision, today), cancellationToken);

    public Task<SalesQuote> AcceptAsync(Guid id, SalesQuoteStatusRequest request, DateOnly today, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, request, (quote, _) => quote.Accept(request.ExpectedRevision, today), cancellationToken);

    public Task<SalesQuote> RejectAsync(Guid id, SalesQuoteStatusRequest request, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, request, (quote, _) => quote.Reject(request.ExpectedRevision), cancellationToken);

    public Task<SalesQuote> CancelAsync(Guid id, SalesQuoteStatusRequest request, CancellationToken cancellationToken = default) =>
        ChangeStatusAsync(id, request, (quote, _) => quote.Cancel(request.ExpectedRevision), cancellationToken);

    public async Task<Sale> ConvertAsync(Guid id, Guid actorId, ConvertSalesQuoteRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var db = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        {
            var quote = await db.SalesQuotes.AsNoTracking().AsSplitQuery().Include(x => x.Lines).SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new DomainException("La cotización no existe en esta empresa.");
            if (quote.Status == SalesQuoteStatus.Converted && quote.ConvertedSaleId is { } convertedId)
            {
                await using var existing = await contextFactory.CreateDbContextAsync(cancellationToken);
                existing.CurrentTenantId = quote.TenantId;
                return await existing.Sales.AsNoTracking().AsSplitQuery().Include(x => x.Lines).Include(x => x.Payments).Include(x => x.Receipts).Include(x => x.Returns)
                    .SingleAsync(x => x.Id == convertedId, cancellationToken);
            }
            var checkout = new CheckoutRequest(quote.CustomerId, PaymentMethod.Credit,
                quote.Lines.Select(x => new CheckoutLine(x.ProductId, x.Quantity)).ToArray(), quote.Id,
                request.RegisterCode, Payments: [], DueDate: request.DueDate, SalesQuoteId: quote.Id, SalesQuoteRevision: request.ExpectedRevision);
            return await storeRepository.CheckoutAsync(checkout, actorId, cancellationToken);
        }
    }

    private async Task<SalesQuote> ChangeStatusAsync(Guid id, SalesQuoteStatusRequest request,
        Action<SalesQuote, StoreDbContext> apply, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(request);
        var db = await OpenTenantContextAsync(cancellationToken);
        await using (db)
        {
            var quote = await db.SalesQuotes.SingleOrDefaultAsync(x => x.Id == id, cancellationToken)
                ?? throw new DomainException("La cotización no existe en esta empresa.");
            apply(quote, db);
            try { await db.SaveChangesAsync(cancellationToken); }
            catch (DbUpdateConcurrencyException) { throw Concurrency(); }
            return quote;
        }
    }

    private static async Task<List<SalesQuoteLine>> BuildLinesAsync(StoreDbContext db, Guid tenantId,
        IReadOnlyCollection<SalesQuoteLineRequest>? requested, CancellationToken cancellationToken)
    {
        if (requested is null || requested.Count is < 1 or > 100) throw new DomainException("Agrega entre 1 y 100 productos a la cotización.");
        if (requested.Any(x => x.ProductId == Guid.Empty || x.Quantity is < 1 or > 100000) || requested.Select(x => x.ProductId).Distinct().Count() != requested.Count)
            throw new DomainException("Cada línea debe tener un producto único y una cantidad válida.");
        var tenant = await db.Tenants.SingleOrDefaultAsync(x => x.Id == tenantId, cancellationToken)
            ?? throw new DomainException("La empresa activa no existe.");
        var ids = requested.Select(x => x.ProductId).ToArray();
        var products = await db.Products.AsNoTracking().Include(x => x.TaxProfile)
            .Where(x => ids.Contains(x.Id) && x.IsActive).ToDictionaryAsync(x => x.Id, cancellationToken);
        if (products.Count != ids.Length) throw new DomainException("Uno de los productos no existe o está inactivo en esta empresa.");
        var lines = new List<SalesQuoteLine>(requested.Count);
        foreach (var item in requested)
        {
            var product = products[item.ProductId];
            if (product.TaxProfileId is not null && product.TaxProfile is not { IsActive: true })
                throw new DomainException($"El impuesto configurado para {product.Name} está inactivo o no existe.");
            var price = item.UnitPrice ?? product.UnitPrice;
            if (price < 0 || price > product.UnitPrice)
                throw new DomainException($"El precio de {product.Name} debe estar entre cero y el precio de lista ({product.UnitPrice:0.00}).");
            var taxRate = product.TaxProfile?.Rate ?? product.TaxRateOverride ?? tenant.DefaultTaxRate;
            lines.Add(new SalesQuoteLine(tenantId, product.Id, product.Sku, product.Name, item.Quantity, price, taxRate));
        }
        return lines;
    }

    private static async Task<string> NextNumberAsync(StoreDbContext db, Guid tenantId, DateOnly today, CancellationToken ct)
    {
        var prefix = $"COT-{today:yyyyMMdd}-";
        var numbers = await db.SalesQuotes.AsNoTracking().Where(x => x.Number.StartsWith(prefix)).Select(x => x.Number).ToListAsync(ct);
        var sequence = numbers.Select(x => int.TryParse(x.AsSpan(prefix.Length), out var value) ? value : 0).DefaultIfEmpty().Max() + 1;
        if (sequence > 9999) throw new DomainException("Se agotó la numeración diaria de cotizaciones.");
        return $"{prefix}{sequence:0000}";
    }

    private async Task<StoreDbContext> OpenTenantContextAsync(CancellationToken cancellationToken)
    {
        var tenantId = tenantContext.GetRequiredTenantId();
        await tenantAccess.EnsureCanOperateAsync(tenantId, tenantContext.CurrentUserId, cancellationToken);
        var db = await contextFactory.CreateDbContextAsync(cancellationToken);
        db.CurrentTenantId = tenantId;
        return db;
    }

    private static ConcurrencyConflictException Concurrency() => new("La cotización cambió en otra sesión. Recarga antes de continuar.");
}
