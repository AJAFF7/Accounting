using SoftMax.Core;
using SoftMax.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;

[Table("GeneralLedger")]
public class GeneralLedger : BaseEntity
{
    [Required, Index]
    public DateTime TransactionDate { get; set; }

    [Required, Index]
    public Guid AccountIdRef { get; set; }

    [StringLength(1000), Required]
    public string Description { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Debit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Credit { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Balance { get; set; }

    [StringLength(100), Index]
    public string ReferenceNumber { get; set; }

    [Index]
    public Guid? JournalEntryIdRef { get; set; }

    [Index]
    public Guid? TransactionIdRef { get; set; }

    [Index]
    public int FiscalYear { get; set; }

    [Index]
    public int FiscalPeriod { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AccountIdRef))]
    public ChartOfAccount Account { get; set; }

    [ForeignKey(nameof(JournalEntryIdRef))]
    public JournalEntry JournalEntry { get; set; }

    [ForeignKey(nameof(TransactionIdRef))]
    public Transaction Transaction { get; set; }
}
