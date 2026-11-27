using System.Text.RegularExpressions;
using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Inventory;

public sealed class Warehouse : TenantEntity
{
    private Warehouse() { }

    public Warehouse(Guid tenantId, string code, string name) : base(tenantId)
    {
        Code = NormalizeCode(code);
        Name = NormalizeName(name);
        IsActive = true;
    }

    public string Code { get; private set; } = "";
    public string Name { get; private set; } = "";
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public void Rename(string name, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        Name = NormalizeName(name);
        AdvanceRevision();
    }

    public void SetActive(bool active, long expectedRevision)
    {
        EnsureRevision(expectedRevision);
        if (IsActive == active) return;
        IsActive = active;
        AdvanceRevision();
    }

    public static string NormalizeCode(string? code)
    {
        var value = code?.Trim().ToUpperInvariant() ?? "";
        if (value.Length is < 1 or > 40 || !Regex.IsMatch(value, "^[A-Z0-9][A-Z0-9_-]*$"))
            throw new DomainException("El código del almacén debe tener hasta 40 caracteres: letras, números, guiones o guiones bajos.");
        return value;
    }

    private static string NormalizeName(string? name)
    {
        var value = name?.Trim() ?? "";
        if (value.Length is < 2 or > 120) throw new DomainException("El nombre del almacén debe tener entre 2 y 120 caracteres.");
        return value;
    }

    private void EnsureRevision(long expectedRevision)
    {
        if (Revision != expectedRevision) throw new ConcurrencyConflictException("El almacén cambió en otra sesión. Recarga la página antes de guardar.");
    }

    private void AdvanceRevision()
    {
        Revision = checked(Revision + 1);
        MarkUpdated();
    }
}

public sealed class WarehouseStock : TenantEntity
{
    private WarehouseStock() { }

    public WarehouseStock(Guid tenantId, Guid warehouseId, Guid productId, int quantity) : base(tenantId)
    {
        if (warehouseId == Guid.Empty || productId == Guid.Empty || quantity < 0)
            throw new DomainException("El saldo de inventario por almacén no es válido.");
        WarehouseId = warehouseId;
        ProductId = productId;
        Quantity = quantity;
    }

    public Guid WarehouseId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }
    public long Revision { get; private set; }

    public void Adjust(int delta)
    {
        if (delta < -Quantity) throw new DomainException("No hay existencias suficientes en este almacén.");
        if (delta > int.MaxValue - Quantity) throw new DomainException("Las existencias de este almacén superan el límite permitido.");
        if (delta == 0) return;
        Quantity += delta;
        Revision = checked(Revision + 1);
        MarkUpdated();
    }
}
