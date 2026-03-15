using SoftMax.Core;
using SoftMax.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;

[Table("Transactions")]
public class Transaction : BaseEntity
{
    [Required, Index]
    public Guid JournalEntryIdRef { get; set; }

    [Required, Index]
    public Guid AccountIdRef { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }

    [Required, Index]
    public TransactionType Type { get; set; }

    [Index]
    public DateTime TransactionDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(JournalEntryIdRef))]
    public JournalEntry JournalEntry { get; set; }

    [ForeignKey(nameof(AccountIdRef))]
    public ChartOfAccount Account { get; set; }
}

public enum TransactionType
{
    Debit,
    Credit
}
