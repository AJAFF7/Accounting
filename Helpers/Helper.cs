using Microsoft.AspNetCore.Components.Forms;
using Microsoft.EntityFrameworkCore;
using MudBlazor;
using SoftMax.Accounting.Models;
using SoftMax.Core;
using System.ComponentModel;

namespace SoftMax.Accounting.Helpers;
public static class Helper
{
    public static async Task<List<AttachmentDataItem>> LoadAttachmentsAsync(this AccountingDbContext context, Guid? sourceId, string sourceName = null, bool readOnly = false)
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            return await context.FileMetadatas.AsNoTracking()
                .Where(a => !a.IsDeleted && a.SourceIdRef == sourceId && (sourceName == null || a.SourceName == sourceName))
                .Select(a => new AttachmentDataItem
                {
                    MetaDataId = a.Id,
                    Extension = a.FileExtension,
                    Name = a.FileTitle,
                    MinioFullPath = a.FullPath,
                    Size = a.FileSize,
                    Saved = true,
                    ReadOnly = readOnly
                })
                .ToListAsync();
        });
    }
    public static async Task<RequestResponse> SaveAsync(this IBrowserFile file,
        RepositoryService<AccountingDbContext> repositoryService,
        Guid? sourceIdRef,
        string sourceName,
        bool append = false,
        StorageType? storageType = null,
        string bucketPath = "/")
    {
        return await TaskHelper.ExecuteTaskAsync<RequestResponse>(async () =>
        {
            // Local helper functions
            async Task<FileMetadata> GetOrCreateFileMetadataAsync(DbContext context, Guid? sourceIdRef, string sourceName, StorageType storageType, bool append)
            {
                var fileMetadata = await context.Set<FileMetadata>()
                    .FirstOrDefaultAsync(a => !a.IsDeleted && !append && a.SourceIdRef == sourceIdRef && a.SourceName == sourceName);

                if (fileMetadata != null) return fileMetadata;

                fileMetadata = new FileMetadata
                {
                    SourceIdRef = sourceIdRef,
                    SourceName = sourceName,
                    Storage = storageType
                };
                await context.Set<FileMetadata>().AddAsync(fileMetadata);
                return fileMetadata;
            }

            void UpdateFileProperties(FileMetadata fileMetadata, IBrowserFile file, string bucketPath, Guid userId)
            {
                var extension = Path.GetExtension(file.Name).TrimStart('.').ToUpperInvariant();
                var fileName = $"{Guid.NewGuid():N}.{extension}".ToUpperInvariant();

                fileMetadata.FileExtension = extension;
                fileMetadata.FileName = fileName;
                fileMetadata.FileTitle = Path.GetFileNameWithoutExtension(file.Name);
                fileMetadata.FileSize = file.Size;
                fileMetadata.FullPath = $"{bucketPath.TrimEnd('/')}/{fileName}";
                fileMetadata.ModifiedByIdRef = userId;
                fileMetadata.ModifiedDate = DateTimeOffset.UtcNow;
            }

            async Task HandleStorageAsync(FileMetadata fileMetadata, IBrowserFile file, UserHelper userHelper, bool append)
            {
                if (fileMetadata.Storage == StorageType.Database)
                    throw new NotSupportedException("Database storage is not supported for file uploads.");

                if (!append)
                    await MinioHelper.DeleteFileAsync(userHelper, fileMetadata.FullPath);

                var uploadResult = await MinioHelper.UploadFileAsync(userHelper, await file.ToFileObjectAsync(file.Size), fileMetadata.FullPath);
                if (!uploadResult.Succeeded)
                    throw new InvalidOperationException("File upload failed.");
            }

            // Early validation
            if (file?.Size == 0)
                return new(false, repositoryService._stringLocalizer["File is empty or not selected."].Value);

            // Determine storage type
            storageType ??= repositoryService._userHelper.Module?.Storage?.StorageType ?? StorageType.S3Storage;

            await using var context = await repositoryService._dbContextFactory.CreateDbContextAsync();

            // Get or create file metadata
            var fileMetadata = await GetOrCreateFileMetadataAsync(context, sourceIdRef, sourceName, storageType.Value, append);

            // Update file properties
            UpdateFileProperties(fileMetadata, file, bucketPath, repositoryService._userHelper.User.Id);

            // Handle storage
            await HandleStorageAsync(fileMetadata, file, repositoryService._userHelper, append);

            await context.SaveChangesAsync(repositoryService);

            return new(true, repositoryService._stringLocalizer["File uploaded successfully."].Value, fileMetadata.Id.ToString()) { Extra1 = fileMetadata.FullPath };
        }, Snackbar: repositoryService._snackbar);
    }
    public static Guid ToGuid(this Enum @enum)
    {
        var fieldInfo = @enum.GetType().GetField(@enum.ToString());
        if (fieldInfo == null)
            throw new ArgumentException($"Enum value '{@enum}' does not have a field.");
        var attribute = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), false)
                                 .FirstOrDefault() as DescriptionAttribute;
        if (attribute == null)
            throw new ArgumentException($"Enum value '{@enum}' does not have a Description attribute.");
        if (!Guid.TryParse(attribute.Description, out var guid))
            throw new ArgumentException($"Description '{attribute.Description}' is not a valid GUID.");
        return guid;
    }
    public static List<DataItem<Guid?>> GetEnumDataItems<TEnum>() where TEnum : Enum
    {
        return Enum.GetValues(typeof(TEnum))
                   .Cast<TEnum>()
                   .Select(e =>
                   {
                       var desc = GetDescriptionFromEnum(e);
                       var guid = Guid.TryParse(desc, out var g) ? g : Guid.Empty;
                       return new DataItem<Guid?>(e.ToString(), guid == Guid.Empty ? (Guid?)null : guid);
                   })
                   .ToList();
    }
    public static string GetEnumNameFromGuid<TEnum>(Guid guid) where TEnum : Enum
    {
        foreach (var value in Enum.GetValues(typeof(TEnum)).Cast<Enum>())
        {
            var desc = GetDescriptionFromEnum(value);
            if (Guid.TryParse(desc, out var g) && g == guid)
                return value.ToString();
        }
        return string.Empty;
    }
    public static bool TryGetEnumFromGuid<TEnum>(Guid guid, out TEnum result) where TEnum : struct, Enum
    {
        foreach (var value in Enum.GetValues(typeof(TEnum)).Cast<Enum>())
        {
            var desc = GetDescriptionFromEnum(value);
            if (Guid.TryParse(desc, out var g) && g == guid)
            {
                result = (TEnum)Enum.Parse(typeof(TEnum), value.ToString());
                return true;
            }
        }
        result = default;
        return false;
    }
    private static string GetDescriptionFromEnum(Enum @enum)
    {
        var fi = @enum.GetType().GetField(@enum.ToString());
        var attr = fi?.GetCustomAttributes(typeof(DescriptionAttribute), false).FirstOrDefault() as DescriptionAttribute;
        return attr?.Description ?? @enum.ToString();
    }
    public static async Task<List<DataItem<Guid?>>> GetLookupsAsync(this RepositoryService<AccountingDbContext> repositoryService, string type)
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await repositoryService._dbContextFactory.CreateDbContextAsync();
            var data = await context.Lookups.AsNoTracking()
                .Where(a => !a.IsDeleted && a.Type == type)
                .ToListAsync();

            return data
            .OrderBy(a => a.Sort)
            .Select(a => new DataItem<Guid?>(a.Name, a.Id, a.LocalizedNames))
            .ToList();
        }, Snackbar: repositoryService._snackbar);
    }
    public static async Task<List<DataItem<Guid?>>> GetLookupsAsync<T>(this RepositoryService<AccountingDbContext> repositoryService, string type, string fieldKey, T fieldValue)
    {
        return await TaskHelper.ExecuteTaskAsync(async () =>
        {
            await using var context = await repositoryService._dbContextFactory.CreateDbContextAsync();
            var data = await context.Lookups.AsNoTracking()
                .Where(a => !a.IsDeleted && a.Type == type)
                .ToListAsync();

            return data.Where(a =>
            {
                if (string.IsNullOrEmpty(fieldKey) || a.Fields is null)
                    return true;

                if (!a.Fields.TryGetValue(fieldKey, out var fieldValueFromDb))
                    return true;

                if (fieldValueFromDb is T directValue)
                    return EqualityComparer<T>.Default.Equals(directValue, fieldValue);

                try
                {
                    var convertedValue = Convert.ChangeType(fieldValueFromDb, typeof(T));
                    return EqualityComparer<T>.Default.Equals((T)convertedValue, fieldValue);
                }
                catch
                {
                    return string.Equals(fieldValueFromDb?.ToString(), fieldValue?.ToString(), StringComparison.OrdinalIgnoreCase);
                }
            })
            .OrderBy(a => a.Sort)
            .Select(a => new DataItem<Guid?>(a.Name, a.Id, a.LocalizedNames))
            .ToList();
        }, Snackbar: repositoryService._snackbar);
    }
    public static async Task<List<DataItem<Guid?>>> LoadLookupsAsync(RepositoryService<AccountingDbContext> repositoryService, string type)
    {
        return await repositoryService.GetLookupsAsync(type);
    }
}
