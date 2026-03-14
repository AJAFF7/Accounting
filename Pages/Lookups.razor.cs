using Microsoft.AspNetCore.Components;
using MudBlazor;
using SoftMax.Accounting.Pages.Dialogs;
using SoftMax.Accounting.Repositories;
using SoftMax.Core;
using SoftMax.Core.Components;

namespace SoftMax.Accounting.Pages;

public partial class Lookups : AuthorizeComponent<Lookups>
{
    [Inject] public RepositoryService<AccountingDbContext> _repositoryService { get; set; }
    private string _selectedType;
    public SoftMaxGrid<LookupRepository.Model, Lookups> SoftMaxGrid { get; set; }
    public LookupRepository Repository => new(_repositoryService);
    public Dictionary<string, Func<LookupRepository.Model, Task>> Functions => new() { { "Edit", async e => await Repository.LoadModelAsync(e, SoftMaxGrid) } };
    public Dictionary<string, Func<LookupRepository.Model, Lookups, UserInformation.Column, RenderFragment>> Templates { get; set; } = new()
    {
        { "NameTemplate", (model, component, column) => builder => builder.AddContent(0, model.Name) }
    };
    private List<DataItem<string>> LookupTypes { get; set; } = [];

    private ToolbarButton<Lookups> toolbarButton;
    protected async override Task OnInitializedAsync()
    {
        LookupTypes = await Repository.GetAvailableTypesAsync();

        toolbarButton = new(
            text: "Operations",
            icon: Icons.Material.Rounded.Task,
            color: Color.Success,
            onClick: async (e, component) =>
            {
                await _repositoryService._dialogService.ShowDialogAsync<LookupOperationDialog>(
                    Localizer["Lookup Operations"].Value,
                    new()
                    {
                        { a => a.Repository, Repository },
                        { a => a.DataGrid, SoftMaxGrid }
                    }, Header: false);
            });
    }
    private async Task SelectedTypeChanged(string type)
    {
        if (!string.IsNullOrEmpty(type))
        {
            _selectedType = type;

            await SoftMaxGrid.FilterAsync(new()
            {
                Active = true,
                Operator = FilterOperator.String.Equal,
                FieldName = nameof(LookupRepository.Model.Type),
                ValueString = _selectedType,
                Filtered = true,
                Type = typeof(string),
                Title = "Type"
            });
        }
        else
        {
            _selectedType = null;
            await SoftMaxGrid.RemoveFilterAsync(nameof(LookupRepository.Model.Type));
        }
    }
}