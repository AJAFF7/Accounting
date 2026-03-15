using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoftMax.Accounting.Repositories;
using SoftMax.Core;
using SoftMax.Core.Components;

namespace SoftMax.Accounting.Pages;

public partial class ChartOfAccounts : AuthorizeComponent<ChartOfAccounts>
{
    [Inject] public RepositoryService<AccountingDbContext> _repositoryService { get; set; }
    
    public SoftMaxGrid<ChartOfAccountRepository.Model, ChartOfAccounts> SoftMaxGrid { get; set; }
    public ChartOfAccountRepository Repository => new(_repositoryService);
    
    public Dictionary<string, Func<ChartOfAccountRepository.Model, Task>> Functions => new() 
    { 
        { "Edit", async e => await Repository.LoadModelAsync(e, SoftMaxGrid) } 
    };
    
    public Dictionary<string, Func<ChartOfAccountRepository.Model, ChartOfAccounts, UserInformation.Column, RenderFragment>> Templates => GetTemplates();

    // Server data callback for grid
    public async Task<GridData<ChartOfAccountRepository.Model>> ServerDataAsync(
        GridState<ChartOfAccountRepository.Model> state, 
        SoftMaxGrid<ChartOfAccountRepository.Model, ChartOfAccounts> grid,
        CancellationToken cancellationToken)
    {
        // Use LoadGridDataSimpleAsync with optional search string
        var allData = await Repository.LoadGridDataSimpleAsync();
        
        // Return grid data with pagination info
        return new GridData<ChartOfAccountRepository.Model>
        {
            Items = allData.Skip(state.Page * state.PageSize).Take(state.PageSize).ToList(),
            TotalItems = allData.Count
        };
    }

    private Dictionary<string, Func<ChartOfAccountRepository.Model, ChartOfAccounts, UserInformation.Column, RenderFragment>> GetTemplates()
    {
        return new()
        {
            { "CodeTemplate", (model, component, column) => builder => builder.AddContent(0, model.Code) },
            { "NameTemplate", (model, component, column) => builder => builder.AddContent(0, model.Name) },
            { "TypeTemplate", (model, component, column) => builder =>
            {
                builder.OpenComponent<MudChip<string>>(0);
                builder.AddAttribute(1, "T", typeof(string));
                builder.AddAttribute(2, "Size", Size.Small);
                builder.AddAttribute(3, "Color", GetTypeColor(model.Type));
                builder.AddAttribute(4, "ChildContent", (RenderFragment)((builder2) => builder2.AddContent(0, model.Type)));
                builder.CloseComponent();
            }},
            { "StatusTemplate", (model, component, column) => builder =>
            {
                builder.OpenComponent<MudChip<string>>(0);
                builder.AddAttribute(1, "T", typeof(string));
                builder.AddAttribute(2, "Size", Size.Small);
                builder.AddAttribute(3, "Color", model.IsActive ? Color.Success : Color.Default);
                builder.AddAttribute(4, "ChildContent", (RenderFragment)((builder2) => builder2.AddContent(0, model.IsActive ? "Active" : "Inactive")));
                builder.CloseComponent();
            }}
        };
    }

    private Color GetTypeColor(string type)
    {
        return type switch
        {
            "Asset" => Color.Success,
            "Liability" => Color.Error,
            "Equity" => Color.Info,
            "Revenue" => Color.Primary,
            "Expense" => Color.Warning,
            _ => Color.Default
        };
    }
}
