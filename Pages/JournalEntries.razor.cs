using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoftMax.Accounting.Repositories;

namespace SoftMax.Accounting.Pages;

public partial class JournalEntries
{
    private List<JournalEntryRepository.Model> entries = new();
    private List<JournalEntryRepository.Model> filteredEntries = new();
    private List<ChartOfAccountRepository.Model> accounts = new();
    private JournalEntryRepository.Model model = new();
    private MudForm form;
    private bool formValid;
    private bool loading = false;
    private bool dialogVisible = false;
    private string dialogTitle = "Create Journal Entry";
    private DateTime? modelDate = DateTime.Today;

    // Filter properties
    private string searchText = "";
    private string statusFilter = "All";
    private DateRange dateRange = null;

    private DialogOptions dialogOptions = new()
    {
        MaxWidth = MaxWidth.Medium,
        FullWidth = true,
        CloseButton = true
    };

    protected override async Task OnInitializedAsync()
    {
        await Task.WhenAll(LoadEntries(), LoadAccounts());
    }

    private async Task LoadEntries()
    {
        loading = true;
        try
        {
            entries = await Repository.GetAllAsync();
            FilterEntries();
        }
        finally
        {
            loading = false;
        }
    }

    private void FilterEntries()
    {
        filteredEntries = entries.Where(e =>
        {
            // Search filter
            if (!string.IsNullOrWhiteSpace(searchText))
            {
                var search = searchText.ToLower();
                var matchesSearch = 
                    (e.Description?.ToLower().Contains(search) ?? false) ||
                    (e.ReferenceNumber?.ToLower().Contains(search) ?? false) ||
                    (e.DebitAccountName?.ToLower().Contains(search) ?? false) ||
                    (e.CreditAccountName?.ToLower().Contains(search) ?? false);
                
                if (!matchesSearch) return false;
            }

            // Status filter
            if (statusFilter != "All")
            {
                if (statusFilter == "Posted" && !e.IsPosted) return false;
                if (statusFilter == "Draft" && e.IsPosted) return false;
            }

            // Date range filter
            if (dateRange?.Start != null && e.Date < dateRange.Start) return false;
            if (dateRange?.End != null && e.Date > dateRange.End) return false;

            return true;
        }).ToList();
    }

    private async Task LoadAccounts()
    {
        accounts = await AccountRepository.GetAllAsync();
    }

    private void OpenAddDialog()
    {
        dialogTitle = "Create Journal Entry";
        model = new JournalEntryRepository.Model
        {
            Date = DateTime.Today
        };
        modelDate = DateTime.Today;
        dialogVisible = true;
    }

    private void OpenEditDialog(JournalEntryRepository.Model entry)
    {
        dialogTitle = "Edit Journal Entry";
        model = new JournalEntryRepository.Model
        {
            Id = entry.Id,
            Date = entry.Date,
            Description = entry.Description,
            DebitAccountIdRef = entry.DebitAccountIdRef,
            CreditAccountIdRef = entry.CreditAccountIdRef,
            Amount = entry.Amount,
            ReferenceNumber = entry.ReferenceNumber
        };
        modelDate = entry.Date;
        dialogVisible = true;
    }

    private void CloseDialog()
    {
        dialogVisible = false;
        model = new();
        modelDate = DateTime.Today;
    }

    private async Task SaveEntry()
    {
        if (!formValid) return;

        // Validate that debit and credit accounts are different
        if (model.DebitAccountIdRef == model.CreditAccountIdRef)
        {
            // Show error - accounts must be different
            return;
        }

        model.Date = modelDate ?? DateTime.Today;
        var success = await Repository.SaveAsync(model);
        if (success)
        {
            CloseDialog();
            await LoadEntries();
        }
    }

    private async Task ConfirmPostEntry(Guid id)
    {
        // TODO: Add confirmation dialog
        await PostEntry(id);
    }

    private async Task PostEntry(Guid id)
    {
        var success = await Repository.PostAsync(id);
        if (success)
        {
            await LoadEntries();
        }
    }

    private async Task ConfirmDeleteEntry(Guid id)
    {
        // TODO: Add confirmation dialog
        await DeleteEntry(id);
    }

    private async Task DeleteEntry(Guid id)
    {
        var success = await Repository.DeleteAsync(id);
        if (success)
        {
            await LoadEntries();
        }
    }
}
