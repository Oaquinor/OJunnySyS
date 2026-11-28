using OJunnySyS.ClassGenerator.Core;

namespace OJunnySyS.Application.Tests;

[TestClass]
public sealed class ClassGeneratorTests
{
    [TestMethod]
    public void Create_ProducesSeparatedLayersAndPreservesTenantAndSchemaRules()
    {
        var files = CodeGenerator.Create(ProductSchema(), null).ToDictionary(x => x.Path, x => x.Content);

        Assert.AreEqual(4, files.Count);
        StringAssert.Contains(files["Domain/Product.cs"], "public sealed class Product : TenantEntity");
        StringAssert.Contains(files["Domain/Product.cs"], "public Product(Guid tenantId, string name, decimal unitPrice, string? note) : base(tenantId)");
        Assert.IsFalse(files["Domain/Product.cs"].Contains("Guid tenantId, Guid tenantId", StringComparison.Ordinal));
        StringAssert.Contains(files["Application/ProductDto.cs"], "string? Note");
        StringAssert.Contains(files["Infrastructure/ProductConfiguration.cs"], ".HasPrecision(18, 4)");
        StringAssert.Contains(files["Infrastructure/ProductConfiguration.cs"], "HasIndex(x => new { x.TenantId, x.Name }).IsUnique()");
        StringAssert.Contains(files["docs/Product-integration.md"], "tenant-scoped");
    }

    [TestMethod]
    public void TypeMapper_RejectsUnknownAndOverPrecisionDecimalColumns()
    {
        var unsupported = new ColumnInfo("Payload", "geometry", "geometry", false, false, null, null, null, null, 1);
        var oversizedDecimal = new ColumnInfo("Amount", "decimal", "decimal(29,4)", false, false, null, 29, 4, null, 1);

        Assert.IsNull(TypeMapper.Map(unsupported));
        Assert.IsNull(TypeMapper.Map(oversizedDecimal));
    }

    [TestMethod]
    public void Create_RejectsInvalidClassNamesFromLibraryCallers()
    {
        Assert.ThrowsExactly<ArgumentException>(() => CodeGenerator.Create(ProductSchema(), "invalid-name"));
        Assert.ThrowsExactly<ArgumentException>(() => CodeGenerator.Create(ProductSchema(), "class"));
        Assert.IsTrue(CodeGenerator.IsValidClassName("CustomerRecord"));
    }

    private static TableSchema ProductSchema()
    {
        ColumnInfo[] columns =
        [
            new("Id", "char", "char(36)", false, false, 36, null, null, null, 1),
            new("TenantId", "char", "char(36)", false, false, 36, null, null, null, 2),
            new("CreatedAtUtc", "datetime", "datetime(6)", false, false, null, null, null, null, 3),
            new("UpdatedAtUtc", "datetime", "datetime(6)", true, false, null, null, null, null, 4),
            new("Name", "varchar", "varchar(120)", false, false, 120, null, null, null, 5),
            new("UnitPrice", "decimal", "decimal(18,4)", false, false, null, 18, 4, null, 6),
            new("Note", "text", "text", true, false, null, null, null, null, 7)
        ];
        KeyInfo[] keys =
        [
            new("PRIMARY", ["Id"], true, "PRIMARY"),
            new("UX_Products_TenantId_Name", ["TenantId", "Name"], true, "INDEX")
        ];
        return new("sales-demo", "Product", "BASE TABLE", columns, keys);
    }
}
