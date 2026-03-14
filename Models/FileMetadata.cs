using SoftMax.Core.Models;
using SoftMax.Core;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SoftMax.Accounting.Models;
[Table("FileMetadatas")]
public class FileMetadata : BaseEntity
{
    [Index] public Guid? SourceIdRef { get; set; }
    [StringLength(150), Index] public string SourceName { get; set; }
    [StringLength(500)] public string FileName { get; set; }
    [StringLength(500)] public string FileTitle { get; set; }
    [StringLength(10)] public string FileExtension { get; set; }
    [Required] public long FileSize { get; set; }
    [Index] public StorageType Storage { get; set; }
    [Index] public Guid? AttachmentIdRef { get; set; }
    public string FullPath { get; set; }
}
