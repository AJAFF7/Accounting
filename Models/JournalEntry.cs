using SoftMax.Core;
using SoftMax.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;

[Table("JournalEntries")]
public class JournalEntry : BaseEntity
{
    [Required, Index]
    public DateTime Date { get; set; }

    [StringLength(1000), Required]
    public string Description { get; set; }

    [Required, Index]
    public Guid DebitAccountIdRef { get; set; }

    [Required, Index]
    public Guid CreditAccountIdRef { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(100), Index]
    public string ReferenceNumber { get; set; }

    [Index]
    public bool IsPosted { get; set; } = false;

    public DateTime? PostedDate { get; set; }

    // Navigation properties
    [ForeignKey(nameof(DebitAccountIdRef))]
    public ChartOfAccount DebitAccount { get; set; }

    [ForeignKey(nameof(CreditAccountIdRef))]
    public ChartOfAccount CreditAccount { get; set; }

    public ICollection<Transaction> Transactions { get; set; }
}
