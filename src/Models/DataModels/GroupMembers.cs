
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Herta.Models.Enums.GroupRole;

namespace Herta.Models.DataModels.GroupMembers
{
    [Table("GroupMembers")]
    public sealed class GroupMembers
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public required int GroupId { get; set; }

        [Required]
        public required int UserId { get; set; }

        [Required]
        public required GroupRole RoleIs { get; set; }

        public DateTime JoinedAt { get; set; }
    }
}