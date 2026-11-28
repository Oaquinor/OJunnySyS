using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Catalog;

/// <summary>A tenant-owned product family. Root entries are groups; one child level represents subgroups.</summary>
public sealed class ProductCategory : TenantEntity
{
    private ProductCategory() { }

    public ProductCategory(Guid tenantId, string name, Guid? parentCategoryId = null) : base(tenantId)
    {
        Name = NormalizeName(name);
        NormalizedName = Normalize(Name);
        ParentCategoryId = parentCategoryId;
        HierarchyKey = parentCategoryId is { } parentId ? $"{parentId:N}|{NormalizedName}" : $"root|{NormalizedName}";
        IsActive = true;
    }

    public string Name { get; private set; } = "";
    public string NormalizedName { get; private set; } = "";
    public string HierarchyKey { get; private set; } = "";
    public Guid? ParentCategoryId { get; private set; }
    public ProductCategory? ParentCategory { get; private set; }
    public bool IsActive { get; private set; }
    public long Revision { get; private set; }

    public string DisplayPath => ParentCategory is null ? Name : $"{ParentCategory.Name} / {Name}";

    public void Deactivate()
    {
        if (!IsActive) return;
        IsActive = false;
        Revision++;
        MarkUpdated();
    }

    public void Reactivate()
    {
        if (IsActive) return;
        IsActive = true;
        Revision++;
        MarkUpdated();
    }

    private static string NormalizeName(string name)
    {
        var value = name?.Trim() ?? "";
        if (value.Length is < 2 or > 80 || value.Contains(" / ", StringComparison.Ordinal))
            throw new DomainException("El nombre del grupo o subgrupo debe tener entre 2 y 80 caracteres y no incluir « / ».");
        return value;
    }

    private static string Normalize(string name) => name.Normalize(System.Text.NormalizationForm.FormKC).ToUpperInvariant();
}
