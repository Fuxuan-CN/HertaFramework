
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Herta.Models.Enums.GroupFileAccess;

namespace Herta.Models.DataModels.File;

[Table("GroupFiles")]
public sealed class UserFile
{
    [Key]
    public int Id { get; set; }

    [Required]
    public int UserId { get; set; }

    [Required]
    public int GroupId { get; set; }

    [Required]
    public GroupFileAccess Access { get; set; }

    [Required]
    public required string FileId { get; set; }

    [Required]
    public required string FileName { get; set; }

    [Required]
    public required string FilePath { get; set; }

    [Required]
    public required string FileHash { get; set; }

    [Required]
    public int FileSize { get; set; }

    [Required]
    public DateTime UploadDate { get; set; }
}