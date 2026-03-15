using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SoftMax.Accounting.Models;
using SoftMax.Accounting.Pages;
using SoftMax.Accounting.Pages.Dialogs;
using SoftMax.Core;
using SoftMax.Core.Components;
using SoftMax.Core.Services;
using System.ComponentModel.DataAnnotations;

namespace SoftMax.Accounting.Repositories;

public class ChartOfAccountRepository(RepositoryService<AccountingDbContext> repositoryService)
{
    private readonly RepositoryService<AccountingDbContext> _repositoryService = repositoryService;

    public bool ModelProcessing { get; set; }
    public bool DataGridLoading { get; set; }

    public async Task AddClickAsync(SoftMaxGrid<Model, ChartOfAccounts> DataGrid)
        => await _repositoryService._dialogService.ShowDialogAsync<ChartOfAccountDialog>(
            _repositoryService._stringLocalizer["New Account"].Value,
            new()
            {
                { x => x.Repository, this },
                { x => x.DataGrid, DataGrid }
            },
            MaxWidth.Medium);

    public async Task LoadModelAsync(Model model, SoftMaxGrid<Model, ChartOfAccounts> DataGrid)
        => await _repositoryService._dialogService.ShowDialogAsync<ChartOfAccountDialog>(
            _repositoryService._stringLocalizer["Edit Account"].Value,
            new()
            {
                { x => x.Repository, this },
                { x => x.Model, model },
                { x => x.DataGrid, DataGrid }
            },
            MaxWidth.Medium);

    public async Task SaveAsync(Model model, MudForm Form, SoftMaxGrid<Model, ChartOfAccounts> DataGrid, IMudDialogInstance Dialog, bool? action = null)
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
            var obj = await context.ChartOfAccounts.FirstOrDefaultAsync(a => a.Id == model.Id);
            if (obj is null)
            {
                obj = new() {  Id = Guid.NewGuid(), CreatedByIdRef = _repositoryService._userHelper.User.Id, CreatedDate = DateTimeOffset.UtcNow };
                await context.ChartOfAccounts.AddAsync(obj);
            }
            else
            {
                obj.ModifiedByIdRef = _repositoryService._userHelper.User.Id;
                obj.ModifiedDate = DateTimeOffset.UtcNow;
            }

            obj.Name = model.Name;
            obj.Code = model.Code;
            obj.Type = Enum.Parse<AccountType>(model.Type);
            obj.Description = model.Description;
            obj.IsActive = model.IsActive;

            await context.SaveChangesAsync(_repositoryService);
            Dialog.Close();
            await DataGrid.Grid.ReloadServerData();
            return true;

        }, () => ModelProcessing = true, () => ModelProcessing = false, _repositoryService._snackbar);
    }

    public async Task DeleteAsync(List<Model> models, SoftMaxGrid<Model, ChartOfAccounts> SoftMaxGrid)
    {
        await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            
            foreach (var model in models)
            {
                var entity = await context.ChartOfAccounts
                    .FirstOrDefaultAsync(x => x.Id == model.Id);

                if (entity != null)
                {
                    entity.IsDeleted = true;
                    entity.DeletedByIdRef = _repositoryService._userHelper.User.Id;
                    entity.DeleteDate = DateTimeOffset.UtcNow;
                }
            }

            await context.SaveChangesAsync(_repositoryService);
            await SoftMaxGrid.Grid.ReloadServerData();
            return true;

        }, () => DataGridLoading = true, () => DataGridLoading = false, _repositoryService._snackbar);
    }

    public async Task<List<Model>> GetAllAsync()
    {
        await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
        return await context.ChartOfAccounts
            .Where(x => !x.IsDeleted)
            .OrderBy(x => x.Code)
            .Select(x => new Model
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Type = x.Type.ToString(),
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<Model>> LoadGridDataSimpleAsync(string searchString = null)
    {
        await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
        
        var query = context.ChartOfAccounts
            .Where(x => !x.IsDeleted)
            .AsQueryable();

        // Apply search filter if provided
        if (!string.IsNullOrWhiteSpace(searchString))
        {
            var searchTerm = searchString.ToLower();
            query = query.Where(x => 
                x.Code.ToLower().Contains(searchTerm) ||
                x.Name.ToLower().Contains(searchTerm) ||
                x.Type.ToString().ToLower().Contains(searchTerm) ||
                (x.Description != null && x.Description.ToLower().Contains(searchTerm)));
        }

        return await query
            .OrderBy(x => x.Code)
            .Select(x => new Model
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Type = x.Type.ToString(),
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<List<Model>> GetByTypeAsync(AccountType type)
    {
        await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
        return await context.ChartOfAccounts
            .Where(x => !x.IsDeleted && x.Type == type && x.IsActive)
            .OrderBy(x => x.Code)
            .Select(x => new Model
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Type = x.Type.ToString(),
                Description = x.Description,
                IsActive = x.IsActive
            })
            .ToListAsync();
    }

    public async Task<Model> GetByIdAsync(Guid id)
    {
        await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
        return await context.ChartOfAccounts
            .Where(x => x.Id == id && !x.IsDeleted)
            .Select(x => new Model
            {
                Id = x.Id,
                Name = x.Name,
                Code = x.Code,
                Type = x.Type.ToString(),
                Description = x.Description,
                IsActive = x.IsActive
            })
            .FirstOrDefaultAsync();
    }

    public async Task<bool> SaveAsync(Model model)
    {
        try
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            ChartOfAccount entity;

            if (model.Id.HasValue && model.Id.Value != Guid.Empty)
            {
                entity = await context.ChartOfAccounts
                    .FirstOrDefaultAsync(x => x.Id == model.Id.Value);

                if (entity == null)
                    return false;

                entity.Name = model.Name;
                entity.Code = model.Code;
                entity.Type = Enum.Parse<AccountType>(model.Type);
                entity.Description = model.Description;
                entity.IsActive = model.IsActive;
                entity.ModifiedByIdRef = _repositoryService._userHelper.User.Id;
                entity.ModifiedDate = DateTimeOffset.UtcNow;
            }
            else
            {
                entity = new ChartOfAccount
                {
                    Id = Guid.NewGuid(),
                    Name = model.Name,
                    Code = model.Code,
                    Type = Enum.Parse<AccountType>(model.Type),
                    Description = model.Description,
                    IsActive = model.IsActive,
                    CreatedByIdRef = _repositoryService._userHelper.User.Id,
                    CreatedDate = DateTimeOffset.UtcNow,
                    IsDeleted = false
                };

                await context.ChartOfAccounts.AddAsync(entity);
            }

            await context.SaveChangesAsync(_repositoryService);
            return true;
        }
        catch (Exception ex)
        {
            _repositoryService._snackbar.Add($"Error saving account: {ex.Message}", MudBlazor.Severity.Error);
            return false;
        }
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        try
        {
            await using var context = await _repositoryService._dbContextFactory.CreateDbContextAsync();
            var entity = await context.ChartOfAccounts
                .FirstOrDefaultAsync(x => x.Id == id);

            if (entity == null)
                return false;

            entity.IsDeleted = true;
            entity.DeletedByIdRef = _repositoryService._userHelper.User.Id;
            entity.DeleteDate = DateTimeOffset.UtcNow;

            await context.SaveChangesAsync(_repositoryService);
            return true;
        }
        catch (Exception ex)
        {
            _repositoryService._snackbar.Add($"Error deleting account: {ex.Message}", MudBlazor.Severity.Error);
            return false;
        }
    }

    public class Model
    {
        public Guid? Id { get; set; }

        [Required(ErrorMessage = "Account name is required")]
        [StringLength(250, ErrorMessage = "Name cannot exceed 250 characters")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Account code is required")]
        [StringLength(50, ErrorMessage = "Code cannot exceed 50 characters")]
        public string Code { get; set; }

        [Required(ErrorMessage = "Account type is required")]
        public string Type { get; set; }

        [StringLength(1000, ErrorMessage = "Description cannot exceed 1000 characters")]
        public string Description { get; set; }

        public bool IsActive { get; set; } = true;
    }
}
