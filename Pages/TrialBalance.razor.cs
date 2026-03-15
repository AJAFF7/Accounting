using SoftMax.Accounting.Repositories;

namespace SoftMax.Accounting.Pages;

public partial class TrialBalance
{
    private GeneralLedgerRepository.TrialBalanceReport report;
    private DateTime? asOfDate = DateTime.Today;
    private bool loading = false;

    private async Task LoadReport()
    {
        loading = true;
        try
        {
            report = await Repository.GetTrialBalanceAsync(asOfDate);
        }
        finally
        {
            loading = false;
        }
    }
}
