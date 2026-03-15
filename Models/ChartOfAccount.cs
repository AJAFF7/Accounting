using SoftMax.Core;
using SoftMax.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;

[Table("ChartOfAccounts")]
public class ChartOfAccount : BaseEntity
{
    [StringLength(250), Required, Index(true, $"\"{nameof(IsDeleted)}\" = FALSE")]
    public string Name { get; set; }

    [StringLength(50), Required, Index]
    public AccountType Type { get; set; }

    [StringLength(50), Index]
    public string Code { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    public bool IsActive { get; set; } = true;

    // Navigation properties
    public ICollection<JournalEntry> DebitJournalEntries { get; set; }
    public ICollection<JournalEntry> CreditJournalEntries { get; set; }
    public ICollection<Transaction> Transactions { get; set; }
}

public enum AccountType
{
    Asset,
    Liability,
    Equity,
    Revenue,
    Expense
}
