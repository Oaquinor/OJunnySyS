using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using OJunnySyS.Domain.Catalog;
using OJunnySyS.Domain.Customers;
using OJunnySyS.Domain.Sales;
using OJunnySyS.Domain.Common;
using OJunnySyS.Domain.Tenancy;
using OJunnySyS.Domain.Purchasing;
using OJunnySyS.Domain.Cash;
using OJunnySyS.Domain.Crm;
using OJunnySyS.Domain.Pos;
using OJunnySyS.Domain.Inventory;
using OJunnySyS.Domain.Accounting;
using OJunnySyS.Domain.Banking;
using OJunnySyS.Infrastructure.Identity;

namespace OJunnySyS.Infrastructure.Persistence;

public sealed class StoreDbContext(DbContextOptions<StoreDbContext> options)
    : IdentityDbContext<ApplicationUser, IdentityRole<Guid>, Guid>(options)
{
    public DbSet<Product> Products => Set<Product>();
    public DbSet<TaxProfile> TaxProfiles => Set<TaxProfile>();
    public DbSet<Customer> Customers => Set<Customer>();
    public DbSet<CustomerTag> CustomerTags => Set<CustomerTag>();
    public DbSet<CustomerTagAssignment> CustomerTagAssignments => Set<CustomerTagAssignment>();
    public DbSet<CustomerSegment> CustomerSegments => Set<CustomerSegment>();
    public DbSet<Sale> Sales => Set<Sale>();
    public DbSet<SalesQuote> SalesQuotes => Set<SalesQuote>();
    public DbSet<SalesQuoteLine> SalesQuoteLines => Set<SalesQuoteLine>();
    public DbSet<ParkedSale> ParkedSales => Set<ParkedSale>();
    public DbSet<ParkedSaleLine> ParkedSaleLines => Set<ParkedSaleLine>();
    public DbSet<SalePayment> SalePayments => Set<SalePayment>();
    public DbSet<CustomerReceipt> CustomerReceipts => Set<CustomerReceipt>();
    public DbSet<SaleReturn> SaleReturns => Set<SaleReturn>();
    public DbSet<SaleReturnLine> SaleReturnLines => Set<SaleReturnLine>();
    public DbSet<TenantPaymentMethod> TenantPaymentMethods => Set<TenantPaymentMethod>();
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<Supplier> Suppliers => Set<Supplier>();
    public DbSet<PurchaseOrder> PurchaseOrders => Set<PurchaseOrder>();
    public DbSet<PurchaseOrderLine> PurchaseOrderLines => Set<PurchaseOrderLine>();
    public DbSet<PurchaseReceiptBatch> PurchaseReceiptBatches => Set<PurchaseReceiptBatch>();
    public DbSet<InventoryMovement> InventoryMovements => Set<InventoryMovement>();
    public DbSet<CashSession> CashSessions => Set<CashSession>();
    public DbSet<CashMovement> CashMovements => Set<CashMovement>();
    public DbSet<Opportunity> Opportunities => Set<Opportunity>();
    public DbSet<CrmActivity> CrmActivities => Set<CrmActivity>();
    public DbSet<SupplierPayment> SupplierPayments => Set<SupplierPayment>();
    public DbSet<PosRegister> PosRegisters => Set<PosRegister>();
    public DbSet<Warehouse> Warehouses => Set<Warehouse>();
    public DbSet<WarehouseStock> WarehouseStocks => Set<WarehouseStock>();
    public DbSet<InventoryAdjustment> InventoryAdjustments => Set<InventoryAdjustment>();
    public DbSet<ChartAccount> ChartAccounts => Set<ChartAccount>();
    public DbSet<JournalEntry> JournalEntries => Set<JournalEntry>();
    public DbSet<JournalEntryLine> JournalEntryLines => Set<JournalEntryLine>();
    public DbSet<BankAccount> BankAccounts => Set<BankAccount>();
    public DbSet<BankReconciliation> BankReconciliations => Set<BankReconciliation>();
    public DbSet<BankStatementLine> BankStatementLines => Set<BankStatementLine>();
    public Guid? CurrentTenantId { get; set; }

    public override int SaveChanges()
    {
        EnforceTenantBoundary();
        return base.SaveChanges();
    }

    public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        EnforceTenantBoundary();
        return base.SaveChangesAsync(cancellationToken);
    }

    private void EnforceTenantBoundary()
    {
        foreach (var entry in ChangeTracker.Entries<TenantEntity>().Where(x => x.State is EntityState.Added or EntityState.Modified))
        {
            if (CurrentTenantId is not { } tenantId || entry.Entity.TenantId != tenantId)
                throw new InvalidOperationException("La operación intenta escribir datos fuera de la empresa activa.");
        }
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.Entity<ApplicationUser>().Property(x => x.DisplayName).HasMaxLength(160).IsRequired();
        modelBuilder.Entity<ApplicationUser>().HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.SetNull);

        modelBuilder.Entity<Tenant>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => x.Slug).IsUnique();
            entity.HasIndex(x => x.OwnerEmail);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Slug).HasMaxLength(80).IsRequired();
            entity.Property(x => x.OwnerEmail).HasMaxLength(254).IsRequired();
            entity.Property(x => x.TimeZoneId).HasMaxLength(100).IsRequired();
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.LegalName).HasMaxLength(160).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(x => x.TaxIdentifier).HasMaxLength(40).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(x => x.BusinessPhone).HasMaxLength(40).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(x => x.BusinessAddress).HasMaxLength(240).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(x => x.ReceiptHeader).HasMaxLength(240).HasDefaultValue(string.Empty).IsRequired();
            entity.Property(x => x.ReceiptFooter).HasMaxLength(240).HasDefaultValue("Gracias por su compra.").IsRequired();
            entity.Property(x => x.ReceiptPaperWidthMm).HasDefaultValue(80);
            entity.Property(x => x.ReceiptShowCustomer).HasDefaultValue(true);
            entity.Property(x => x.ReceiptShowSku).HasDefaultValue(true);
            entity.Property(x => x.ReceiptShowTax).HasDefaultValue(true);
            entity.Property(x => x.ReceiptDesign).HasMaxLength(20).HasDefaultValue("classic").IsRequired();
            entity.Property(x => x.ReceiptCompactLineSpacing).HasDefaultValue(false);
            entity.Property(x => x.ReceiptFontFamily).HasMaxLength(20).HasDefaultValue("modern").IsRequired();
            entity.Property(x => x.ReceiptFontSize).HasDefaultValue(12);
            entity.Property(x => x.ReceiptMarginLeftMm).HasDefaultValue(0);
            entity.Property(x => x.ReceiptMarginRightMm).HasDefaultValue(0);
            entity.Property(x => x.ReceiptShowUnitPrice).HasDefaultValue(true);
            entity.Property(x => x.ReceiptShowLineTotals).HasDefaultValue(true);
            entity.Property(x => x.DefaultTaxRate).HasPrecision(5, 4).HasDefaultValue(0.18m);
            entity.Property(x => x.PlanCode).HasMaxLength(40).IsRequired();
        });

        modelBuilder.Entity<TenantPaymentMethod>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.LedgerAccountId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Instructions).HasMaxLength(300);
            entity.Property(x => x.AccountLabel).HasMaxLength(120);
            entity.Property(x => x.AccountReference).HasMaxLength(100);
            entity.Property(x => x.QrPayload).HasMaxLength(2000);
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
        });

        modelBuilder.Entity<TaxProfile>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.IsActive, x.Name });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(24).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Rate).HasPrecision(5, 4);
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
        });

        modelBuilder.Entity<Product>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.Property(x => x.Barcode).HasMaxLength(64);
            entity.HasIndex(x => new { x.TenantId, x.Sku }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Barcode }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.IsActive, x.Name });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne(x => x.TaxProfile).WithMany().HasForeignKey(x => new { x.TenantId, x.TaxProfileId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Category).HasMaxLength(80);
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.UnitCost).HasPrecision(14, 4).HasDefaultValue(0m);
            entity.Property(x => x.UncostedQuantity).HasDefaultValue(0);
            entity.Property(x => x.TaxRateOverride).HasPrecision(5, 4);
            entity.Property(x => x.Revision).IsConcurrencyToken();
            entity.Property(x => x.Revision).HasDefaultValue(0L);
        });

        modelBuilder.Entity<Customer>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Email }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.IsActive, x.FirstName, x.LastName });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.FirstName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.LastName).HasMaxLength(100);
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(40);
            entity.Property(x => x.Company).HasMaxLength(160);
            entity.Property(x => x.LifetimeValue).HasPrecision(14, 2);
            entity.Property(x => x.Revision).IsConcurrencyToken();
        });

        modelBuilder.Entity<CustomerTag>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Name).HasMaxLength(40).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Color).HasConversion<int>();
        });

        modelBuilder.Entity<CustomerTagAssignment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CustomerId, x.CustomerTagId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.CustomerTagId, x.CustomerId });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Cascade);
            entity.HasOne<CustomerTag>().WithMany().HasForeignKey(x => x.CustomerTagId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
        });

        modelBuilder.Entity<CustomerSegment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.NormalizedName }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.RequiredTagId });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CustomerTag>().WithMany().HasForeignKey(x => x.RequiredTagId).OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Name).HasMaxLength(100).IsRequired();
            entity.Property(x => x.NormalizedName).HasMaxLength(100).IsRequired();
            entity.Property(x => x.MinimumLifetimeValue).HasPrecision(14, 2);
            entity.Property(x => x.MaximumLifetimeValue).HasPrecision(14, 2);
            entity.Property(x => x.Revision).IsConcurrencyToken();
        });

        modelBuilder.Entity<Sale>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.CustomerId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.WarehouseId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.SalesQuoteId }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.ParkedSaleId }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SalesQuote>().WithMany().HasForeignKey(x => new { x.TenantId, x.SalesQuoteId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ParkedSale>().WithMany().HasForeignKey(x => new { x.TenantId, x.ParkedSaleId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(64).IsRequired();
            entity.Property(x => x.RegisterCode).HasMaxLength(40).IsRequired();
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.IdempotencyKey).IsRequired(false);
            entity.Property(x => x.CustomerName).HasMaxLength(220).IsRequired();
            entity.Property(x => x.SellerName).HasMaxLength(160);
            entity.HasIndex(x => new { x.TenantId, x.SellerUserId, x.CreatedAtUtc });
            entity.Property(x => x.Subtotal).HasPrecision(14, 2);
            entity.Ignore(x => x.GrossSubtotal);
            entity.Ignore(x => x.DiscountAmount);
            entity.Property(x => x.TaxAmount).HasPrecision(14, 2);
            entity.Property(x => x.Total).HasPrecision(14, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 4);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany<SaleLine>(nameof(Sale.Lines)).WithOne().HasForeignKey(line => line.SaleId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(nameof(Sale.Lines)).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany<SalePayment>(nameof(Sale.Payments)).WithOne().HasForeignKey(x => new { x.TenantId, x.SaleId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(nameof(Sale.Payments)).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany<CustomerReceipt>(nameof(Sale.Receipts)).WithOne().HasForeignKey(x => new { x.TenantId, x.SaleId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(nameof(Sale.Receipts)).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasMany<SaleReturn>(nameof(Sale.Returns)).WithOne().HasForeignKey(x => new { x.TenantId, x.SaleId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(nameof(Sale.Returns)).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ParkedSale>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.RegisterCode, x.Status, x.CreatedAtUtc });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.RegisterCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(220).IsRequired();
            entity.Property(x => x.SellerName).HasMaxLength(160);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
            entity.HasMany<ParkedSaleLine>(nameof(ParkedSale.Lines)).WithOne().HasForeignKey(x => new { x.TenantId, x.ParkedSaleId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(nameof(ParkedSale.Lines)).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<ParkedSaleLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.ParkedSaleId });
            entity.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.DiscountPercent).HasPrecision(5, 2);
        });

        modelBuilder.Entity<SalesQuote>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.CustomerId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.Status, x.ValidUntil });
            entity.HasIndex(x => new { x.TenantId, x.ConvertedSaleId }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => new { x.TenantId, x.CustomerId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Sale>().WithMany().HasForeignKey(x => new { x.TenantId, x.ConvertedSaleId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(40).IsRequired();
            entity.Property(x => x.CustomerName).HasMaxLength(220).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.Subtotal).HasPrecision(14, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(14, 2);
            entity.Property(x => x.Total).HasPrecision(14, 2);
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
            entity.HasMany<SalesQuoteLine>(nameof(SalesQuote.Lines)).WithOne().HasForeignKey(x => new { x.TenantId, x.SalesQuoteId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(nameof(SalesQuote.Lines)).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SalesQuoteLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.SalesQuoteId });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.LineTotal).HasPrecision(14, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 4);
            entity.Ignore(x => x.TaxAmount);
        });

        modelBuilder.Entity<SaleReturn>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.SaleId, x.CreatedAtUtc });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantPaymentMethod>().WithMany().HasForeignKey(x => new { x.TenantId, x.PaymentMethodId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.LedgerAccountId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PaymentMethodName).HasMaxLength(80);
            entity.Property(x => x.Subtotal).HasPrecision(14, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(14, 2);
            entity.Property(x => x.Total).HasPrecision(14, 2);
            entity.Property(x => x.ReceivableCredit).HasPrecision(14, 2);
            entity.Property(x => x.RefundAmount).HasPrecision(14, 2);
            entity.HasMany<SaleReturnLine>(nameof(SaleReturn.Lines)).WithOne().HasForeignKey(x => new { x.TenantId, x.SaleReturnId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(nameof(SaleReturn.Lines)).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<SaleReturnLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.SaleLineId });
            entity.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<SaleLine>().WithMany().HasForeignKey(x => x.SaleLineId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.DiscountPercent).HasPrecision(5, 2).HasDefaultValue(0m);
            entity.Property(x => x.DiscountAmount).HasPrecision(14, 2).HasDefaultValue(0m);
            entity.Property(x => x.UnitCost).HasPrecision(14, 4);
            entity.Property(x => x.LineTotal).HasPrecision(14, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 4);
            entity.Property(x => x.TaxAmount).HasPrecision(14, 2);
            entity.Property(x => x.CostTotal).HasPrecision(14, 2);
        });

        modelBuilder.Entity<CustomerReceipt>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.CustomerId, x.CreatedAtUtc });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.LedgerAccountId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantPaymentMethod>().WithMany().HasForeignKey(x => new { x.TenantId, x.PaymentMethodId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Amount).HasPrecision(14, 2);
            entity.Property(x => x.MethodName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.MethodType).HasConversion<int>();
            entity.Property(x => x.Reference).HasMaxLength(120);
        });

        modelBuilder.Entity<SaleLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.UnitPrice).HasPrecision(12, 2);
            entity.Property(x => x.DiscountPercent).HasPrecision(5, 2).HasDefaultValue(0m);
            entity.Property(x => x.DiscountAmount).HasPrecision(14, 2).HasDefaultValue(0m);
            entity.Property(x => x.UnitCost).HasPrecision(14, 4).HasDefaultValue(0m);
            entity.Property(x => x.UncostedQuantity).HasDefaultValue(0);
            entity.Ignore(x => x.GrossLineTotal);
            entity.Ignore(x => x.CostedQuantity);
            entity.Property(x => x.LineTotal).HasPrecision(14, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 4);
            entity.Ignore(x => x.TaxAmount);
        });

        modelBuilder.Entity<SalePayment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.SaleId });
            entity.HasIndex(x => new { x.TenantId, x.PaymentMethodId, x.Type });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantPaymentMethod>().WithMany().HasForeignKey(x => new { x.TenantId, x.PaymentMethodId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.LedgerAccountId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.MethodName).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(120);
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.Amount).HasPrecision(14, 2);
        });

        modelBuilder.Entity<Supplier>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Name });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Name).HasMaxLength(160).IsRequired();
            entity.Property(x => x.TaxIdentifier).HasMaxLength(40);
            entity.Property(x => x.Email).HasMaxLength(254).IsRequired();
            entity.Property(x => x.Phone).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Notes).HasMaxLength(1000);
            entity.Property(x => x.Revision).IsConcurrencyToken();
        });

        modelBuilder.Entity<PurchaseOrder>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.CreatedAtUtc });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Supplier>().WithMany().HasForeignKey(x => x.SupplierId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(64).IsRequired();
            entity.Property(x => x.SupplierName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Reference).HasMaxLength(100);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Subtotal).HasPrecision(14, 2);
            entity.Property(x => x.TaxAmount).HasPrecision(14, 2);
            entity.Property(x => x.Total).HasPrecision(14, 2);
            entity.HasMany<PurchaseOrderLine>(nameof(PurchaseOrder.Lines)).WithOne().HasForeignKey(line => line.PurchaseOrderId).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(nameof(PurchaseOrder.Lines)).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<PurchaseOrderLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.Property(x => x.Sku).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.UnitCost).HasPrecision(12, 2);
            entity.Property(x => x.LineTotal).HasPrecision(14, 2);
            entity.Property(x => x.TaxRate).HasPrecision(5, 4);
        });

        modelBuilder.Entity<PurchaseReceiptBatch>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.PurchaseOrderId, x.CreatedAtUtc });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PurchaseOrder>().WithMany().HasForeignKey(x => new { x.TenantId, x.PurchaseOrderId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.RequestFingerprint).HasMaxLength(12000).IsRequired();
        });

        modelBuilder.Entity<InventoryMovement>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.WarehouseId, x.ProductId, x.CreatedAtUtc });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => x.ProductId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Reference).HasMaxLength(100).IsRequired();
        });

        modelBuilder.Entity<CashSession>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.RegisterCode, x.Status });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PosRegister>().WithMany().HasForeignKey(x => new { x.TenantId, x.RegisterCode })
                .HasPrincipalKey(x => new { x.TenantId, x.Code }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.RegisterCode).HasMaxLength(40).IsRequired();
            entity.Property(x => x.OpeningFloat).HasPrecision(14, 2);
            entity.Property(x => x.ExpectedCash).HasPrecision(14, 2);
            entity.Property(x => x.CountedCash).HasPrecision(14, 2);
        });

        modelBuilder.Entity<PosRegister>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Code });
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
        });

        modelBuilder.Entity<Warehouse>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasAlternateKey(x => new { x.TenantId, x.Code });
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
        });

        modelBuilder.Entity<WarehouseStock>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.WarehouseId, x.ProductId }).IsUnique();
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
        });

        modelBuilder.Entity<InventoryAdjustment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.WarehouseId, x.CreatedAtUtc });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Product>().WithMany().HasForeignKey(x => new { x.TenantId, x.ProductId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Warehouse>().WithMany().HasForeignKey(x => new { x.TenantId, x.WarehouseId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.CounterAccountId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(40).IsRequired();
            entity.Property(x => x.RequestFingerprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.ProductSku).HasMaxLength(40).IsRequired();
            entity.Property(x => x.ProductName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.WarehouseName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.ActorName).HasMaxLength(160).IsRequired();
            entity.Property(x => x.CountedQuantity);
            entity.Property(x => x.UnitCost).HasPrecision(14, 4);
            entity.Property(x => x.ValuationAmount).HasPrecision(14, 2);
            entity.Property(x => x.Reason).HasMaxLength(240).IsRequired();
            entity.Property(x => x.CounterAccountCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.CounterAccountName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.JournalEntryNumber).HasMaxLength(40);
        });

        modelBuilder.Entity<ChartAccount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Code }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Code).HasMaxLength(20).IsRequired();
            entity.Property(x => x.Name).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240);
            entity.Property(x => x.Type).HasConversion<int>();
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
        });

        modelBuilder.Entity<BankAccount>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Name }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.LedgerAccountId }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.LedgerAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Name).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Institution).HasMaxLength(100).IsRequired();
            entity.Property(x => x.AccountSuffix).HasMaxLength(4);
            entity.Property(x => x.CurrencyCode).HasMaxLength(3).IsRequired();
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
        });

        modelBuilder.Entity<JournalEntry>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.Number }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.EntryDate, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.SourceType, x.SourceId }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PostedByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Number).HasMaxLength(40).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(240).IsRequired();
            entity.Property(x => x.SourceType).HasMaxLength(40);
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.TotalDebit).HasPrecision(14, 2);
            entity.Property(x => x.TotalCredit).HasPrecision(14, 2);
            entity.HasMany<JournalEntryLine>(nameof(JournalEntry.Lines)).WithOne().HasForeignKey(x => new { x.TenantId, x.JournalEntryId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Cascade);
            entity.Navigation(nameof(JournalEntry.Lines)).UsePropertyAccessMode(PropertyAccessMode.Field);
        });

        modelBuilder.Entity<JournalEntryLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.AccountId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.AccountCode).HasMaxLength(20).IsRequired();
            entity.Property(x => x.AccountName).HasMaxLength(120).IsRequired();
            entity.Property(x => x.Debit).HasPrecision(14, 2);
            entity.Property(x => x.Credit).HasPrecision(14, 2);
            entity.Property(x => x.Memo).HasMaxLength(160);
        });

        modelBuilder.Entity<BankReconciliation>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasAlternateKey(x => new { x.TenantId, x.Id });
            entity.HasIndex(x => new { x.TenantId, x.BankAccountId, x.StatementNumber }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.BankAccountId, x.PeriodEnd });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BankAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.BankAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasMany<BankStatementLine>(nameof(BankReconciliation.Lines)).WithOne()
                .HasForeignKey(x => new { x.TenantId, x.BankReconciliationId }).HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.Navigation(nameof(BankReconciliation.Lines)).UsePropertyAccessMode(PropertyAccessMode.Field);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.StatementNumber).HasMaxLength(80).IsRequired();
            entity.Property(x => x.Status).HasConversion<int>();
            entity.Property(x => x.OpeningBalance).HasPrecision(14, 2);
            entity.Property(x => x.ClosingBalance).HasPrecision(14, 2);
            entity.Property(x => x.Revision).IsConcurrencyToken().HasDefaultValue(0L);
        });

        modelBuilder.Entity<BankStatementLine>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.BankReconciliationId, x.TransactionDate });
            entity.HasIndex(x => new { x.TenantId, x.JournalEntryLineId }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<BankReconciliation>().WithMany(nameof(BankReconciliation.Lines))
                .HasForeignKey(x => new { x.TenantId, x.BankReconciliationId }).HasPrincipalKey(x => new { x.TenantId, x.Id })
                .OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<JournalEntryLine>().WithMany().HasForeignKey(x => new { x.TenantId, x.JournalEntryLineId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Description).HasMaxLength(180).IsRequired();
            entity.Property(x => x.Amount).HasPrecision(14, 2);
            entity.Property(x => x.Reference).HasMaxLength(120);
        });

        modelBuilder.Entity<CashMovement>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.SessionId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CashSession>().WithMany().HasForeignKey(x => x.SessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<TenantPaymentMethod>().WithMany().HasForeignKey(x => new { x.TenantId, x.CashPaymentMethodId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.CounterAccountId })
                .HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Amount).HasPrecision(14, 2);
            entity.Property(x => x.Description).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<Opportunity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.Stage, x.ExpectedCloseDate });
            entity.HasIndex(x => new { x.TenantId, x.AssignedToUserId });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Title).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Description).HasMaxLength(2000);
            entity.Property(x => x.ExpectedValue).HasPrecision(14, 2);
            entity.Property(x => x.Stage).HasConversion<int>();
        });

        modelBuilder.Entity<CrmActivity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.CustomerId, x.OccurredAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.Kind, x.DueAtUtc, x.CompletedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.AssignedToUserId, x.Kind, x.DueAtUtc, x.CompletedAtUtc });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Customer>().WithMany().HasForeignKey(x => x.CustomerId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<Opportunity>().WithMany().HasForeignKey(x => x.OpportunityId).OnDelete(DeleteBehavior.SetNull);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.AssignedToUserId).OnDelete(DeleteBehavior.SetNull);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Kind).HasConversion<int>();
            entity.Property(x => x.Subject).HasMaxLength(160).IsRequired();
            entity.Property(x => x.Details).HasMaxLength(2000);
        });

        modelBuilder.Entity<SupplierPayment>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.HasIndex(x => new { x.TenantId, x.IdempotencyKey }).IsUnique();
            entity.HasIndex(x => new { x.TenantId, x.PurchaseOrderId, x.CreatedAtUtc });
            entity.HasIndex(x => new { x.TenantId, x.LedgerAccountId });
            entity.HasOne<Tenant>().WithMany().HasForeignKey(x => x.TenantId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<PurchaseOrder>().WithMany(nameof(PurchaseOrder.Payments)).HasForeignKey(x => x.PurchaseOrderId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ApplicationUser>().WithMany().HasForeignKey(x => x.PaidByUserId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<CashSession>().WithMany().HasForeignKey(x => x.CashSessionId).OnDelete(DeleteBehavior.Restrict);
            entity.HasOne<ChartAccount>().WithMany().HasForeignKey(x => new { x.TenantId, x.LedgerAccountId }).HasPrincipalKey(x => new { x.TenantId, x.Id }).OnDelete(DeleteBehavior.Restrict);
            entity.HasQueryFilter(x => CurrentTenantId != null && x.TenantId == CurrentTenantId);
            entity.Property(x => x.Amount).HasPrecision(14, 2);
            entity.Property(x => x.Method).HasConversion<int>();
            entity.Property(x => x.Reference).HasMaxLength(100);
            entity.Property(x => x.RegisterCode).HasMaxLength(40).IsRequired();
        });

        // Oracle's MySQL EF provider reads DATE columns as DateTime. Keep the
        // domain model's DateOnly semantics while converting at the provider edge.
        var dateOnlyConverter = new ValueConverter<DateOnly, DateTime>(
            value => value.ToDateTime(TimeOnly.MinValue),
            value => DateOnly.FromDateTime(value));
        var nullableDateOnlyConverter = new ValueConverter<DateOnly?, DateTime?>(
            value => value.HasValue ? value.Value.ToDateTime(TimeOnly.MinValue) : null,
            value => value.HasValue ? DateOnly.FromDateTime(value.Value) : null);

        foreach (var entityType in modelBuilder.Model.GetEntityTypes())
        foreach (var property in entityType.GetProperties())
        {
            if (property.ClrType == typeof(DateOnly))
            {
                property.SetValueConverter(dateOnlyConverter);
                property.SetColumnType("date");
            }
            else if (Nullable.GetUnderlyingType(property.ClrType) == typeof(DateOnly))
            {
                property.SetValueConverter(nullableDateOnlyConverter);
                property.SetColumnType("date");
            }
        }
    }
}
