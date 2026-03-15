using Microsoft.EntityFrameworkCore;
using SoftMax.Accounting.Models;
using SoftMax.Core;
using SoftMax.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace SoftMax.Accounting.Repositories;

public class JournalEntryRepository(RepositoryService<AccountingDbContext> repositoryService)
{
    private readonly RepositoryService<AccountingDbContext> _repositoryService = repositoryService;

    public async Task<List<Model>> GetAllAsync()
    {
        await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .Where(x => !x.IsDeleted)
            .OrderByDescending(x => x.Date)
            .Select(x => new Model
            {
                Id = x.Id,
                Date = x.Date,
                Description = x.Description,
                DebitAccountIdRef = x.DebitAccountIdRef,
                CreditAccountIdRef = x.CreditAccountIdRef,
                Amount = x.Amount,
                ReferenceNumber = x.ReferenceNumber,
                IsPosted = x.IsPosted,
                PostedDate = x.PostedDate,
                DebitAccountName = x.DebitAccount.Name,
                CreditAccountName = x.CreditAccount.Name
            })
            .ToListAsync();
    }

    public async Task<Model> GetByIdAsync(Guid id)
    {
        await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
        return await context.JournalEntries
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new Model
            {
                Id = x.Id,
                Date = x.Date,
                Description = x.Description,
                DebitAccountIdRef = x.DebitAccountIdRef,
                CreditAccountIdRef = x.CreditAccountIdRef,
                Amount = x.Amount,
                ReferenceNumber = x.ReferenceNumber,
                IsPosted = x.IsPosted,
                PostedDate = x.PostedDate,
                DebitAccountName = x.DebitAccount.Name,
                CreditAccountName = x.CreditAccount.Name
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SaveAsync(Model model)
    {
        try
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            JournalEntry entity;

            if (model.Id.HasValue && model.Id.Value != Guid.Empty)
            {
                entity = await context.JournalEntries
                    .FirstOrDefaultAsync(x => x.Id == model.Id.Value);

                if (entity == null)
                    return false;

                // Don't allow editing posted entries
                if (entity.IsPosted)
                {
                    _repositoryService._snackbar.Add("Cannot edit a posted journal entry", MudBlazor.Severity.Warning);
                    return false;
                }

                entity.Date = model.Date;
                entity.Description = model.Description;
                entity.DebitAccountIdRef = model.DebitAccountIdRef;
                entity.CreditAccountIdRef = model.CreditAccountIdRef;
                entity.Amount = model.Amount;
                entity.ReferenceNumber = model.ReferenceNumber;
                entity.ModifiedByIdRef = _repositoryService._userHelper.User.Id;
                entity.ModifiedDate = DateTimeOffset.UtcNow;
            }
            else
            {
                entity = new JournalEntry
                {
                    Id = Guid.NewGuid(),
                    Date = model.Date,
                    Description = model.Description,
                    DebitAccountIdRef = model.DebitAccountIdRef,
                    CreditAccountIdRef = model.CreditAccountIdRef,
                    Amount = model.Amount,
                    ReferenceNumber = model.ReferenceNumber,
                    IsPosted = false,
                    CreatedByIdRef = _repositoryService._userHelper.User.Id,
                    CreatedDate = DateTimeOffset.UtcNow,
                    IsDeleted = false
                };

                await context.JournalEntries.AddAsync(entity);
            }

            await context.SaveChangesAsync(_repositoryService);
            return true;
        }
        catch (Exception ex)
        {
            _repositoryService._snackbar.Add($"Error saving journal entry: {ex.Message}", MudBlazor.Severity.Error);
            return false;
        }
    }

    public async Task<bool> PostAsync(Guid id)
    {
        try
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            var entry = await context.JournalEntries
                .Include(x => x.DebitAccount)
                .Include(x => x.CreditAccount)
                .FirstOrDefaultAsync(x => x.Id == id && !x.IsDeleted);

            if (entry == null)
                return false;

            if (entry.IsPosted)
            {
                _repositoryService._snackbar.Add("Entry is already posted", MudBlazor.Severity.Warning);
                return false;
            }

            // Create transactions
            var debitTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                JournalEntryIdRef = entry.Id,
                AccountIdRef = entry.DebitAccountIdRef,
                Amount = entry.Amount,
                Type = TransactionType.Debit,
                TransactionDate = entry.Date,
                Balance = entry.Amount, // Simplified - should calculate actual balance
                CreatedByIdRef = _repositoryService._userHelper.User.Id,
                CreatedDate = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            var creditTransaction = new Transaction
            {
                Id = Guid.NewGuid(),
                JournalEntryIdRef = entry.Id,
                AccountIdRef = entry.CreditAccountIdRef,
                Amount = entry.Amount,
                Type = TransactionType.Credit,
                TransactionDate = entry.Date,
                Balance = -entry.Amount, // Simplified - should calculate actual balance
                CreatedByIdRef = _repositoryService._userHelper.User.Id,
                CreatedDate = DateTimeOffset.UtcNow,
                IsDeleted = false
            };

            await context.Transactions.AddRangeAsync(debitTransaction, creditTransaction);

            entry.IsPosted = true;
            entry.PostedDate = DateTimeOffset.UtcNow.DateTime;
            entry.ModifiedByIdRef = _repositoryService._userHelper.User.Id;
            entry.ModifiedDate = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(_repositoryService);
            return true;
        }
        catch (Exception ex)
        {
            _repositoryService._snackbar.Add($"Error posting journal entry: {ex.Message}", MudBlazor.Severity.Error);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            var entity = await context.JournalEntries
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            if (entity.IsPosted)
            {
                _repositoryService._snackbar.Add("Cannot delete a posted journal entry", MudBlazor.Severity.Warning);
                return false;
            }

            entity.IsDeleted = true;
            entity.DeletedByIdRef = _repositoryService._userHelper.User.Id;
            entity.DeleteDate = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(_repositoryService);
            return true;
        }
        catch (Exception ex)
        {
            _repositoryService._snackbar.Add($"Error deleting journal entry: {ex.Message}", MudBlazor.Severity.Error);
            return false;
        }
    }

    public class Model
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Date is required")]
        public DateTime Date { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        [Required(ErrorMessage = "Debit account is required")]
        public Guid DebitAccountIdRef { get; set; }

        [Required(ErrorMessage = "Credit account is required")]
        public Guid CreditAccountIdRef { get; set; }

        [Required(ErrorMessage = "Amount is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Amount must be greater than 0")]
        public decimal Amount { get; set; }

        [StringLength(100)]
        public string ReferenceNumber { get; set; }

        public bool IsPosted { get; set; }
        public DateTime? PostedDate { get; set; }

        // Display properties
        public string DebitAccountName { get; set; }
        public string CreditAccountName { get; set; }
    }
}
