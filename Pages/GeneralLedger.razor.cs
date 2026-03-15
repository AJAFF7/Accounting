using Microsoft.AspNetCore.Components;
using SoftMax.Accounting.Repositories;

namespace SoftMax.Accounting.Pages;

public partial class GeneralLedger
{
    private List<ChartOfAccountRepository.Model> accounts = new();
    private List<GeneralLedgerRepository.LedgerEntry> ledgerEntries;
    private ChartOfAccountRepository.Model selectedAccount;
    private Guid? selectedAccountId;
    private DateTime? startDate;
    private DateTime? endDate;
    private bool loading = false;

    protected override async Task OnInitializedAsync()
    {
        await LoadAccounts();
    }

    private async Task LoadAccounts()
    {
        accounts = await AccountRepository.GetAllAsync();
    }

    private async Task LoadLedger()
    {
        if (selectedAccountId == null) return;

        loading = true;
        try
        {
            ledgerEntries = await Repository.GetByAccountAsync(
                selectedAccountId.Value, 
                startDate, 
                endDate
            );
            
            selectedAccount = accounts.FirstOrDefault(a => a.Id == selectedAccountId);
        }
        finally
        {
            loading = false;
        }
    }
}
