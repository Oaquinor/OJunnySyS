using OJunnySyS.Domain.Common;

namespace OJunnySyS.Domain.Accounting;

public enum JournalEntryStatus { Posted }

public sealed class JournalEntry : TenantEntity
{
    private readonly List<JournalEntryLine> _lines = [];
    private JournalEntry() { }

    public JournalEntry(Guid tenantId, string number, DateOnly entryDate, string description, Guid postedByUserId,
        IEnumerable<JournalEntryLine> lines, string? sourceType = null, Guid? sourceId = null) : base(tenantId)
    {
        if (string.IsNullOrWhiteSpace(number) || number.Trim().Length > 40) throw new DomainException("El número del asiento no es válido.");
        if (entryDate == default) throw new DomainException("La fecha del asiento es obligatoria.");
        if (string.IsNullOrWhiteSpace(description) || description.Trim().Length is < 3 or > 240) throw new DomainException("La descripción debe tener entre 3 y 240 caracteres.");
        if (postedByUserId == Guid.Empty) throw new DomainException("El usuario que registra el asiento no es válido.");
        if ((sourceType is null) != (sourceId is null)) throw new DomainException("El origen del asiento debe incluir tipo e identificador.");
        if (sourceType?.Trim().Length > 40) throw new DomainException("El origen del asiento no puede superar 40 caracteres.");
        _lines.AddRange(lines);
        if (_lines.Count is < 2 or > 200) throw new DomainException("Un asiento debe tener entre 2 y 200 líneas.");
        if (_lines.Any(x => x.TenantId != tenantId)) throw new DomainException("Todas las líneas deben pertenecer a la empresa activa.");
        if (_lines.Select(x => x.AccountId).Distinct().Count() < 2) throw new DomainException("Un asiento debe afectar al menos dos cuentas distintas.");
        TotalDebit = decimal.Round(_lines.Sum(x => x.Debit), 2, MidpointRounding.AwayFromZero);
        TotalCredit = decimal.Round(_lines.Sum(x => x.Credit), 2, MidpointRounding.AwayFromZero);
        if (TotalDebit <= 0 || TotalDebit != TotalCredit) throw new DomainException("El asiento debe cuadrar: débitos y créditos deben ser iguales y mayores que cero.");
        if (TotalDebit > 999999999999.99m) throw new DomainException("El importe del asiento supera el máximo permitido.");
        Number = number.Trim();
        EntryDate = entryDate;
        Description = description.Trim();
        PostedByUserId = postedByUserId;
        SourceType = sourceType?.Trim().ToUpperInvariant();
        SourceId = sourceId;
        Status = JournalEntryStatus.Posted;
    }

    public string Number { get; private set; } = "";
    public DateOnly EntryDate { get; private set; }
    public string Description { get; private set; } = "";
    public Guid PostedByUserId { get; private set; }
    public string? SourceType { get; private set; }
    public Guid? SourceId { get; private set; }
    public JournalEntryStatus Status { get; private set; }
    public decimal TotalDebit { get; private set; }
    public decimal TotalCredit { get; private set; }
    public IReadOnlyCollection<JournalEntryLine> Lines => _lines;
}

public sealed class JournalEntryLine : TenantEntity
{
    private JournalEntryLine() { }

    public JournalEntryLine(Guid tenantId, Guid accountId, string accountCode, string accountName,
        decimal debit, decimal credit, string? memo = null) : base(tenantId)
    {
        if (accountId == Guid.Empty) throw new DomainException("Selecciona una cuenta válida.");
        if (string.IsNullOrWhiteSpace(accountCode) || accountCode.Length > 20 || string.IsNullOrWhiteSpace(accountName) || accountName.Length > 120)
            throw new DomainException("La cuenta asociada a la línea no es válida.");
        if (debit < 0 || credit < 0 || (debit == 0) == (credit == 0)) throw new DomainException("Cada línea debe tener un único débito o crédito mayor que cero.");
        if (debit > 999999999999.99m || credit > 999999999999.99m || debit != decimal.Round(debit, 2) || credit != decimal.Round(credit, 2))
            throw new DomainException("Los importes contables deben tener hasta dos decimales y estar dentro del límite permitido.");
        if (memo?.Trim().Length > 160) throw new DomainException("El detalle de la línea no puede superar 160 caracteres.");
        AccountId = accountId;
        AccountCode = accountCode.Trim();
        AccountName = accountName.Trim();
        Debit = debit;
        Credit = credit;
        Memo = string.IsNullOrWhiteSpace(memo) ? null : memo.Trim();
    }

    public Guid JournalEntryId { get; private set; }
    public Guid AccountId { get; private set; }
    public string AccountCode { get; private set; } = "";
    public string AccountName { get; private set; } = "";
    public decimal Debit { get; private set; }
    public decimal Credit { get; private set; }
    public string? Memo { get; private set; }
}
