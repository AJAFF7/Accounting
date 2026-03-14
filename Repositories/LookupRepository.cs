using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SoftMax.Accounting.Pages;
using SoftMax.Accounting.Pages.Dialogs;
using SoftMax.Core;
using SoftMax.Core.Components;
using System.ComponentModel.DataAnnotations;

namespace SoftMax.Accounting.Repositories;
public class LookupRepository(RepositoryService<AccountingDbContext> repositoryService)
{
    public bool ModelProcessing { get; set; }
    public bool DataGridLoading { get; set; }
    public RepositoryService<AccountingDbContext> _repositoryService { get; set; } = repositoryService;

    public async Task AddClickAsync(SoftMaxGrid<Model, Lookups> DataGrid)
        => await _repositoryService._dialogService.ShowDialogAsync<LookupsDialog>(
            _repositoryService._stringLocalizer["New Lookup Item"].Value,
            new()
            {
                { x => x.Repository, this },
                { x => x.DataGrid, DataGrid }
            },
            MaxWidth.Large);

    public async Task SaveAsync(Model model, MudForm Form, SoftMaxGrid<Model, Lookups> DataGrid, IMudDialogInstance Dialog, bool? action = null)
    {
        await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await Form.ValidateAsync();
            if (!Form.IsValid)
            {
                _repositoryService._snackbar.Add(_repositoryService._stringLocalizer["Error: Check Validations !"].Value, Severity.Warning);
                return false;
            }

            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            var obj = await context.Lookups.FirstOrDefaultAsync(a => a.Id == model.Id);
            if (obj is null)
            {
                obj = new() { CreatedByIdRef = _repositoryService._userHelper.User.Id };
                await context.Lookups.AddAsync(obj);
            }
            else
            {
                obj.ModifiedByIdRef = _repositoryService._userHelper.User.Id;
                obj.ModifiedDate = DateTimeOffset.UtcNow;
            }

            obj.Name = model.Name;
            obj.Type = model.Type;
            obj.Sort = model.Sort;
            obj.Fields = model.Fields;
            obj.LocalizedNames = model.LocalizedNames;

            await context.SaveChangesAsync(_repositoryService);

            _repositoryService._snackbar.Add(_repositoryService._stringLocalizer["Saved Successfully !"].Value, Severity.Success);
            await DataGrid.Grid.ReloadServerData();
            if (action == true) Dialog.Close();
            else if (action == false)
            {
                model = new();
                await Form.ResetAsync();
            }
            else model.Id = obj.Id;
            return true;
        }, () => ModelProcessing = true, () => ModelProcessing = false, _repositoryService._snackbar);
    }

    public async Task LoadModelAsync(Model model, SoftMaxGrid<Model, Lookups> DataGrid)
    {
        await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            var Model = await context.Lookups.Where(a => a.Id == model.Id).AsNoTracking().Select(a => new Model
            {
                Id = a.Id,
                Name = a.Name,
                Type = a.Type,
                Sort = a.Sort,
                Fields = a.Fields,
                LocalizedNames = a.LocalizedNames
            }).FirstOrDefaultAsync();
            if (Model is not null)
            {
                await _repositoryService._dialogService.ShowDialogAsync<LookupsDialog>($"Edit: {Model.Name}", new() {
                        { x => x.Model, Model },
                        { x => x.DataGrid, DataGrid },
                        { x => x.Repository, this } }, MaxWidth.Large);
            }
            else _repositoryService._snackbar.Add($"Error: User Not Found.", Severity.Error);
        }, () => DataGridLoading = true, () => DataGridLoading = false, _repositoryService._snackbar);
    }

    public async Task<List<DataItem<string>>> GetAvailableLocalesAsync()
    {
        return await TaskHelper.ExecuteTaskAsync<List<DataItem<string>>>(async () =>
        {
            await using var context = await _repositoryService._adminDbContext.CreateDbContextAsync();
            return await context.Languages.AsNoTracking()
                .Where(a => !a.IsDeleted)
                .OrderBy(a => a.Sort)
                .Select(a => new DataItem<string>(a.Name, a.Locale))
                .ToListAsync();
        });
    }

    public async Task DeleteAsync(List<Model> models, SoftMaxGrid<Model, Lookups> SoftMaxGrid)
    {
        await TaskHelper.ExecuteTaskAsync(async () =>
        {
            var Ids = models.Select(a => a.Id).ToArray();
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            var Models = await context.Lookups.Where(a => Ids.Contains(a.Id)).ToListAsync();
            foreach (var x in Models)
            {
                x.IsDeleted = true;
                x.DeleteDate = DateTimeOffset.UtcNow;
                x.DeletedByIdRef = _repositoryService._userHelper.User.Id;
            }
            await context.SaveChangesAsync(_repositoryService);
            _repositoryService._snackbar.Add(_repositoryService._stringLocalizer["Deleted Successfully !"].Value, Severity.Success);
            await SoftMaxGrid.Grid.ReloadServerData();
        }, () => DataGridLoading = true, () => DataGridLoading = false, _repositoryService._snackbar);
    }

    public async Task<List<DataItem<string>>> GetAvailableTypesAsync()
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            return await context.Lookups.AsNoTracking()
                .Where(a => !a.IsDeleted && !string.IsNullOrEmpty(a.Type))
                .GroupBy(a => a.Type)
                .OrderBy(a => a.Key)
                .Select(a => new DataItem<string>(a.Key, a.Key))
                .ToListAsync();
        });
    }

    public async Task BatchOperationClickAsync(SoftMaxGrid<Model, Lookups> DataGrid)
        => await _repositoryService._dialogService.ShowDialogAsync<LookupOperationDialog>(
            _repositoryService._stringLocalizer["Batch Operations on Fields"].Value,
            new()
            {
                { x => x.Repository, this },
                { x => x.DataGrid, DataGrid }
            },
            MaxWidth.Large);

    public async Task<List<string>> GetAvailableFieldKeysAsync()
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            var allFields = await context.Lookups.AsNoTracking()
                .Where(a => !a.IsDeleted && a.Fields != null)
                .Select(a => a.Fields)
                .ToListAsync();
            
            return allFields.SelectMany(f => f.Keys).Distinct().OrderBy(k => k).ToList();
        });
    }

    public async Task<List<string>> GetCurrentValuesForFieldAsync(string fieldKey)
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            
            var allFields = await context.Lookups.AsNoTracking()
                .Where(a => !a.IsDeleted && a.Fields != null && a.Fields.ContainsKey(fieldKey))
                .Select(a => a.Fields[fieldKey])
                .ToListAsync();
            
            return allFields.Distinct().OrderBy(v => v).ToList();
        });
    }

    public async Task BatchAddFieldAsync(string type, string key, string value)
    {
        await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            
            var query = context.Lookups.Where(a => !a.IsDeleted);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);
            
            var lookups = await query.ToListAsync();
            
            foreach (var lookup in lookups)
            {
                if (lookup.Fields == null)
                    lookup.Fields = new Dictionary<string, string>();
                
                lookup.Fields[key] = value;
                lookup.ModifiedByIdRef = _repositoryService._userHelper.User.Id;
                lookup.ModifiedDate = DateTimeOffset.UtcNow;
            }
            
            await context.SaveChangesAsync(_repositoryService);
            
            _repositoryService._snackbar.Add(
                _repositoryService._stringLocalizer["Field added successfully to {0} records"].Value.Replace("{0}", lookups.Count.ToString()),
                Severity.Success);
        }, () => ModelProcessing = true, () => ModelProcessing = false, _repositoryService._snackbar);
    }

    public async Task BatchRemoveFieldAsync(string type, string key, string currentValueFilter = null)
    {
        await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            
            var query = context.Lookups.Where(a => !a.IsDeleted);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);
            
            var lookups = await query.ToListAsync();
            int affectedCount = 0;
            
            foreach (var lookup in lookups)
            {
                if (lookup.Fields != null && lookup.Fields.ContainsKey(key))
                {
                    // If currentValueFilter is specified, only remove fields that match the current value
                    if (!string.IsNullOrEmpty(currentValueFilter) && lookup.Fields[key] != currentValueFilter)
                        continue;
                    
                    lookup.Fields.Remove(key);
                    lookup.ModifiedByIdRef = _repositoryService._userHelper.User.Id;
                    lookup.ModifiedDate = DateTimeOffset.UtcNow;
                    affectedCount++;
                }
            }
            
            await context.SaveChangesAsync(_repositoryService);
            
            var message = string.IsNullOrEmpty(currentValueFilter) 
                ? _repositoryService._stringLocalizer["Field removed from {0} records"].Value.Replace("{0}", affectedCount.ToString())
                : _repositoryService._stringLocalizer["Field removed from {0} records (filtered by current value)"].Value.Replace("{0}", affectedCount.ToString());
            
            _repositoryService._snackbar.Add(message, Severity.Success);
        }, () => ModelProcessing = true, () => ModelProcessing = false, _repositoryService._snackbar);
    }

    public async Task<int> GetRecordCountAsync(string type = null)
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            
            var query = context.Lookups.AsNoTracking().Where(a => !a.IsDeleted);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);
            
            return await query.CountAsync();
        });
    }

    public async Task<int> GetRecordCountWithFieldAsync(string key, string type = null)
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            
            var query = context.Lookups.AsNoTracking().Where(a => !a.IsDeleted);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);
            
            var lookups = await query.ToListAsync();
            return lookups.Count(l => l.Fields != null && l.Fields.ContainsKey(key));
        });
    }

    public async Task<int> GetRecordCountWithFieldValueAsync(string key, string currentValue, string type = null)
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            
            var query = context.Lookups.AsNoTracking().Where(a => !a.IsDeleted);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);
            
            var lookups = await query.ToListAsync();
            return lookups.Count(l => l.Fields != null && l.Fields.ContainsKey(key) && l.Fields[key] == currentValue);
        });
    }

    public async Task BatchUpdateFieldAsync(string type, string key, string value, string currentValueFilter = null)
    {
        await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            
            var query = context.Lookups.Where(a => !a.IsDeleted);
            if (!string.IsNullOrEmpty(type))
                query = query.Where(a => a.Type == type);
            
            var lookups = await query.ToListAsync();
            int affectedCount = 0;
            
            foreach (var lookup in lookups)
            {
                if (lookup.Fields != null && lookup.Fields.ContainsKey(key))
                {
                    // If currentValueFilter is specified, only update fields that match the current value
                    if (!string.IsNullOrEmpty(currentValueFilter) && lookup.Fields[key] != currentValueFilter)
                        continue;
                    
                    lookup.Fields[key] = value;
                    lookup.ModifiedByIdRef = _repositoryService._userHelper.User.Id;
                    lookup.ModifiedDate = DateTimeOffset.UtcNow;
                    affectedCount++;
                }
            }
            
            await context.SaveChangesAsync(_repositoryService);
            
            var message = string.IsNullOrEmpty(currentValueFilter) 
                ? _repositoryService._stringLocalizer["Field updated in {0} records"].Value.Replace("{0}", affectedCount.ToString())
                : _repositoryService._stringLocalizer["Field updated in {0} records (filtered by current value)"].Value.Replace("{0}", affectedCount.ToString());
            
            _repositoryService._snackbar.Add(message, Severity.Success);
        }, () => ModelProcessing = true, () => ModelProcessing = false, _repositoryService._snackbar);
    }

    public class Model
    {
        [Key] public Guid? Id { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }
        public long? Sort { get; set; }
        public Dictionary<string, string> LocalizedNames { get; set; } = [];
        public Dictionary<string, string> Fields { get; set; } = [];


        public void SetLocalizedName(string locale, string localizedName)
        {
            if (string.IsNullOrEmpty(locale))
                return;

            if (LocalizedNames is null)
                LocalizedNames = new Dictionary<string, string>();

            LocalizedNames[locale] = localizedName;
        }
        public void SetField(string key, string value)
        {
            if (string.IsNullOrEmpty(key))
                return;

            if (Fields is null)
                Fields = new Dictionary<string, string>();

            Fields[key] = value;
        }
    }
}
