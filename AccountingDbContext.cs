using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SoftMax.Accounting.Models;
using SoftMax.Core;

namespace SoftMax.Accounting;
public class AccountingDbContext : DbContext
{
    public string Schema { get { return nameof(AccountingDbContext).Replace("DbContext", ""); } }
    protected bool LogEnabled { get; } = true;

    [ActivatorUtilitiesConstructor] public AccountingDbContext(DbContextOptions<AccountingDbContext> options) : base(options) { }
    public AccountingDbContext(DbContextOptions<AccountingDbContext> options, bool LogEnabled) : this(options) => this.LogEnabled = LogEnabled;

    public DbSet<FileMetadata> FileMetadatas { get; set; }
    public DbSet<Lookup> Lookups { get; set; }
    public DbSet<ChartOfAccount> ChartOfAccounts { get; set; }
    public DbSet<JournalEntry> JournalEntries { get; set; }
    public DbSet<Transaction> Transactions { get; set; }
    public DbSet<GeneralLedger> GeneralLedgers { get; set; }
    public DbSet<AccountsPayable> AccountsPayables { get; set; }
    public DbSet<AccountsPayablePayment> AccountsPayablePayments { get; set; }
    public DbSet<AccountsReceivable> AccountsReceivables { get; set; }
    public DbSet<AccountsReceivablePayment> AccountsReceivablePayments { get; set; }
    public DbSet<FixedAsset> FixedAssets { get; set; }
    public DbSet<FixedAssetDepreciation> FixedAssetDepreciations { get; set; }

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);   
        builder.OnModelCreating(GetType());
        
        // Configure enum to string conversion for AccountType
        builder.Entity<ChartOfAccount>()
            .Property(e => e.Type)
            .HasConversion<string>();
        
        // Configure JournalEntry relationships
        builder.Entity<JournalEntry>()
            .HasOne(je => je.DebitAccount)
            .WithMany(ca => ca.DebitJournalEntries)
            .HasForeignKey(je => je.DebitAccountIdRef)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<JournalEntry>()
            .HasOne(je => je.CreditAccount)
            .WithMany(ca => ca.CreditJournalEntries)
            .HasForeignKey(je => je.CreditAccountIdRef)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure Transaction relationships
        builder.Entity<Transaction>()
            .HasOne(t => t.JournalEntry)
            .WithMany(je => je.Transactions)
            .HasForeignKey(t => t.JournalEntryIdRef)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Entity<Transaction>()
            .HasOne(t => t.Account)
            .WithMany(ca => ca.Transactions)
            .HasForeignKey(t => t.AccountIdRef)
            .OnDelete(DeleteBehavior.Restrict);

        // Configure GeneralLedger relationships
        builder.Entity<GeneralLedger>()
            .HasOne(gl => gl.Account)
            .WithMany()
            .HasForeignKey(gl => gl.AccountIdRef)
            .OnDelete(DeleteBehavior.Restrict);

        builder.Entity<GeneralLedger>()
            .HasOne(gl => gl.JournalEntry)
            .WithMany()
            .HasForeignKey(gl => gl.JournalEntryIdRef)
            .OnDelete(DeleteBehavior.SetNull);

        // Configure AccountsPayable relationships
        builder.Entity<AccountsPayable>()
            .HasOne(ap => ap.ExpenseAccount)
            .WithMany()
            .HasForeignKey(ap => ap.ExpenseAccountIdRef)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<AccountsPayablePayment>()
            .HasOne(app => app.AccountsPayable)
            .WithMany(ap => ap.Payments)
            .HasForeignKey(app => app.AccountsPayableIdRef)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure AccountsReceivable relationships
        builder.Entity<AccountsReceivable>()
            .HasOne(ar => ar.RevenueAccount)
            .WithMany()
            .HasForeignKey(ar => ar.RevenueAccountIdRef)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<AccountsReceivablePayment>()
            .HasOne(arp => arp.AccountsReceivable)
            .WithMany(ar => ar.Payments)
            .HasForeignKey(arp => arp.AccountsReceivableIdRef)
            .OnDelete(DeleteBehavior.Cascade);

        // Configure FixedAsset relationships
        builder.Entity<FixedAsset>()
            .HasOne(fa => fa.AssetAccount)
            .WithMany()
            .HasForeignKey(fa => fa.AssetAccountIdRef)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<FixedAsset>()
            .HasOne(fa => fa.DepreciationAccount)
            .WithMany()
            .HasForeignKey(fa => fa.DepreciationAccountIdRef)
            .OnDelete(DeleteBehavior.SetNull);

        builder.Entity<FixedAssetDepreciation>()
            .HasOne(fad => fad.FixedAsset)
            .WithMany(fa => fa.DepreciationSchedule)
            .HasForeignKey(fad => fad.FixedAssetIdRef)
            .OnDelete(DeleteBehavior.Cascade);

        base.OnModelCreating(builder);
    }
    public async Task<int> SaveChangesAsync(RepositoryService<AccountingDbContext> repositoryService)
    {
        if (LogEnabled && (repositoryService._userHelper?.Module?.LogEnabled == true || repositoryService?._userHelper?.Module is null))
            this.LogAsync(repositoryService._userHelper, repositoryService._navigationManager);

        var affectedRows = await base.SaveChangesAsync();

        if (affectedRows <= 0 && repositoryService?._snackbar is not null)
            repositoryService._snackbar.Add(repositoryService._stringLocalizer["No changes were made to the database."].Value, Severity.Info);

        return affectedRows;
    }
}