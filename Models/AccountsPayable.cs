using SoftMax.Core;
using SoftMax.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;

[Table("AccountsPayable")]
public class AccountsPayable : BaseEntity
{
    [StringLength(250), Required, Index]
    public string VendorName { get; set; }

    [StringLength(100)]
    public string VendorCode { get; set; }

    [StringLength(100), Required, Index]
    public string InvoiceNumber { get; set; }

    [Required, Index]
    public DateTime InvoiceDate { get; set; }

    [Required, Index]
    public DateTime DueDate { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal TotalAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountPaid { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AmountDue { get; set; }

    [StringLength(50), Required, Index]
    public PaymentStatus Status { get; set; }

    [StringLength(100)]
    public string PaymentTerms { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [StringLength(100)]
    public string PurchaseOrderNumber { get; set; }

    public DateTime? PaymentDate { get; set; }

    [StringLength(100)]
    public string PaymentReference { get; set; }

    [Index]
    public Guid? ExpenseAccountIdRef { get; set; }

    // Navigation properties
    [ForeignKey(nameof(ExpenseAccountIdRef))]
    public ChartOfAccount ExpenseAccount { get; set; }

    public ICollection<AccountsPayablePayment> Payments { get; set; }
}

[Table("AccountsPayablePayments")]
public class AccountsPayablePayment : BaseEntity
{
    [Required, Index]
    public Guid AccountsPayableIdRef { get; set; }

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
    [ForeignKey(nameof(AccountsPayableIdRef))]
    public AccountsPayable AccountsPayable { get; set; }
}

public enum PaymentStatus
{
    Pending,
    Partial,
    Paid,
    Overdue,
    Cancelled
}
