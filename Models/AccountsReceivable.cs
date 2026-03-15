using SoftMax.Core;
using SoftMax.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;

[Table("AccountsReceivable")]
public class AccountsReceivable : BaseEntity
{
    [StringLength(250), Required, Index]
    public string CustomerName { get; set; }

    [StringLength(100)]
    public string CustomerCode { get; set; }

    [StringLength(100), Required, Index]
    public string InvoiceNumber { get; set; }

    [Required, Index]
    public DateTime InvoiceDate { get; set; }

    [Required, Index]
    public DateTime DueDate { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountReceived { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountDue { get; set; }

    [StringLength(50), Required, Index]
    public PaymentStatus Status { get; set; }

    [StringLength(100)]
    public string PaymentTerms { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(100)]
    public string SalesOrderNumber { get; set; }

    public DateTime? PaymentDate { get; set; }

    [StringLength(100)]
    public string PaymentReference { get; set; }

    [Index]
    public Guid? RevenueAccountIdRef { get; set; }

    // Navigation properties
    [ForeignKey(nameof(RevenueAccountIdRef))]
    public ChartOfAccount RevenueAccount { get; set; }

    public ICollection<AccountsReceivablePayment> Payments { get; set; }
}

[Table("AccountsReceivablePayments")]
public class AccountsReceivablePayment : BaseEntity
{
    [Required, Index]
    public Guid AccountsReceivableIdRef { get; set; }

    [Required]
    public DateTime PaymentDate { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal Amount { get; set; }

    [StringLength(100)]
    public string PaymentMethod { get; set; }

    [StringLength(100)]
    public string ReferenceNumber { get; set; }

    [StringLength(500)]
    public string Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AccountsReceivableIdRef))]
    public AccountsReceivable AccountsReceivable { get; set; }
}
