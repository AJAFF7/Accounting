using SoftMax.Core;
using SoftMax.Core.Models;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;

[Table("FixedAssets")]
public class FixedAsset : BaseEntity
{
    [StringLength(250), Required, Index(true, $"\"{nameof(IsDeleted)}\" = FALSE")]
    public string AssetName { get; set; }

    [StringLength(100), Required, Index]
    public string AssetNumber { get; set; }

    [StringLength(100), Required, Index]
    public string Category { get; set; }

    [StringLength(1000)]
    public string Description { get; set; }

    [Required, Index]
    public DateTime PurchaseDate { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal PurchaseCost { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal SalvageValue { get; set; }

    [Required]
    public int UsefulLifeYears { get; set; }

    [StringLength(50), Required]
    public DepreciationMethod DepreciationMethod { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AccumulatedDepreciation { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal CurrentValue { get; set; }

    [StringLength(250)]
    public string Location { get; set; }

    [StringLength(100)]
    public string SerialNumber { get; set; }

    [StringLength(250)]
    public string Manufacturer { get; set; }

    [StringLength(100)]
    public string Model { get; set; }

    [Index]
    public AssetStatus Status { get; set; }

    public DateTime? DisposalDate { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal? DisposalAmount { get; set; }

    [StringLength(500)]
    public string DisposalNotes { get; set; }

    [Index]
    public Guid? AssetAccountIdRef { get; set; }

    [Index]
    public Guid? DepreciationAccountIdRef { get; set; }

    // Navigation properties
    [ForeignKey(nameof(AssetAccountIdRef))]
    public ChartOfAccount AssetAccount { get; set; }

    [ForeignKey(nameof(DepreciationAccountIdRef))]
    public ChartOfAccount DepreciationAccount { get; set; }

    public ICollection<FixedAssetDepreciation> DepreciationSchedule { get; set; }
}

[Table("FixedAssetDepreciation")]
public class FixedAssetDepreciation : BaseEntity
{
    [Required, Index]
    public Guid FixedAssetIdRef { get; set; }

    [Required, Index]
    public DateTime DepreciationDate { get; set; }

    [Required]
    public int FiscalYear { get; set; }

    [Required]
    public int FiscalPeriod { get; set; }

    [Required, Column(TypeName = "decimal(18,2)")]
    public decimal DepreciationAmount { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal AccumulatedDepreciation { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal BookValue { get; set; }

    [StringLength(500)]
    public string Notes { get; set; }

    // Navigation properties
    [ForeignKey(nameof(FixedAssetIdRef))]
    public FixedAsset FixedAsset { get; set; }
}

public enum DepreciationMethod
{
    StraightLine,
    DecliningBalance,
    DoubleDecline,
    SumOfYears,
    UnitsOfProduction
}

public enum AssetStatus
{
    Active,
    InMaintenance,
    Disposed,
    Retired,
    UnderConstruction
}
