using Microsoft.EntityFrameworkCore;
using SoftMax.Accounting.Models;
using SoftMax.Core;
using SoftMax.Core.Services;

namespace SoftMax.Accounting.Repositories;

public class GeneralLedgerRepository(RepositoryService<AccountingDbContext> repositoryService)
{
    private readonly RepositoryService<AccountingDbContext> _repositoryService = repositoryService;

    public async Task<List<LedgerEntry>> GetByAccountAsync(Guid accountId, DateTime? startDate = null, DateTime? endDate = null)
    {
        await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
        
        var query = context.Transactions
            .Where(x => !x.IsDeleted && x.AccountIdRef == accountId);

        if (startDate.HasValue)
            query = query.Where(x => x.TransactionDate >= startDate.Value);
        
        if (endDate.HasValue)
            query = query.Where(x => x.TransactionDate <= endDate.Value);

        var transactions = await query
            .Include(x => x.JournalEntry)
            .OrderBy(x => x.TransactionDate)
            .ThenBy(x => x.CreatedDate)
            .ToListAsync();

        // Calculate running balance
        decimal runningBalance = 0;
        var ledgerEntries = new List<LedgerEntry>();

        foreach (var txn in transactions)
        {
            runningBalance += txn.Type == TransactionType.Debit ? txn.Amount : -txn.Amount;
            
            ledgerEntries.Add(new LedgerEntry
            {
                TransactionId = txn.Id,
                Date = txn.TransactionDate,
                Description = txn.JournalEntry?.Description ?? "Transaction",
                ReferenceNumber = txn.JournalEntry?.ReferenceNumber,
                Debit = txn.Type == TransactionType.Debit ? txn.Amount : 0,
                Credit = txn.Type == TransactionType.Credit ? txn.Amount : 0,
                Balance = runningBalance,
                JournalEntryId = txn.JournalEntryIdRef
            });
        }

        return ledgerEntries;
    }

    public async Task<List<AccountSummary>> GetAccountSummariesAsync(DateTime? asOfDate = null)
    {
        await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
        
        var query = context.Transactions
            .Where(x => !x.IsDeleted);

        if (asOfDate.HasValue)
            query = query.Where(x => x.TransactionDate <= asOfDate.Value);

        var accountBalances = await query
            .GroupBy(x => x.AccountIdRef)
            .Select(g => new
            {
                AccountId = g.Key,
                TotalDebit = g.Where(x => x.Type == TransactionType.Debit).Sum(x => x.Amount),
                TotalCredit = g.Where(x => x.Type == TransactionType.Credit).Sum(x => x.Amount),
                TransactionCount = g.Count()
            })
            .ToListAsync();

        var accountIds = accountBalances.Select(x => x.AccountId).ToList();
        var accounts = await context.ChartOfAccounts
            .Where(x => accountIds.Contains(x.Id) && !x.IsDeleted)
            .ToDictionaryAsync(x => x.Id);

        return accountBalances.Select(ab => new AccountSummary
        {
            AccountId = ab.AccountId,
            AccountCode = accounts.ContainsKey(ab.AccountId) ? accounts[ab.AccountId].Code : "",
            AccountName = accounts.ContainsKey(ab.AccountId) ? accounts[ab.AccountId].Name : "",
            AccountType = accounts.ContainsKey(ab.AccountId) ? accounts[ab.AccountId].Type.ToString() : "",
            TotalDebit = ab.TotalDebit,
            TotalCredit = ab.TotalCredit,
            Balance = ab.TotalDebit - ab.TotalCredit,
            TransactionCount = ab.TransactionCount
        })
        .OrderBy(x => x.AccountCode)
        .ToList();
    }

    public async Task<TrialBalanceReport> GetTrialBalanceAsync(DateTime? asOfDate = null)
    {
        var summaries = await GetAccountSummariesAsync(asOfDate);
        
        var report = new TrialBalanceReport
        {
            AsOfDate = asOfDate ?? DateTime.Today,
            Accounts = summaries,
            TotalDebit = summaries.Sum(x => x.TotalDebit),
            TotalCredit = summaries.Sum(x => x.TotalCredit),
            IsBalanced = Math.Abs(summaries.Sum(x => x.TotalDebit) - summaries.Sum(x => x.TotalCredit)) < 0.01m
        };

        return report;
    }

    public class LedgerEntry
    {
        public Guid TransactionId { get; set; }
        public DateTime Date { get; set; }
        public string Description { get; set; }
        public string ReferenceNumber { get; set; }
        public decimal Debit { get; set; }
        public decimal Credit { get; set; }
        public decimal Balance { get; set; }
        public Guid JournalEntryId { get; set; }
    }

    public class AccountSummary
    {
        public Guid AccountId { get; set; }
        public string AccountCode { get; set; }
        public string AccountName { get; set; }
        public string AccountType { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public decimal Balance { get; set; }
        public int TransactionCount { get; set; }
    }

    public class TrialBalanceReport
    {
        public DateTime AsOfDate { get; set; }
        public List<AccountSummary> Accounts { get; set; }
        public decimal TotalDebit { get; set; }
        public decimal TotalCredit { get; set; }
        public bool IsBalanced { get; set; }
    }
}
