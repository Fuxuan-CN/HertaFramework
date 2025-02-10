
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herta.Models.DataModels.Groups
{
    [Table("GroupsTable")]
    public sealed class Groups
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required string GroupName { get; set; }

        [Required]
        public required string Description { get; set; }

        [StringLength(255, ErrorMessage = "群头像URL长度不能超过255")]
        public string? AvatarUrl { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}