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

    protected override void OnModelCreating(ModelBuilder builder)
    {
        builder.HasDefaultSchema(Schema);   
        builder.OnModelCreating(GetType());
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