using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Herta.Models.DataModels.UserInfos
{
    [Table("UserInfos")] // 映射到表 UserInfos
    public sealed class UserInfo
    {
        [Key]
        public int Id { get; set; } // Primary key
        
        public int UserId { get; set; } // Foreign key

        [Required]
        [StringLength(50)]
        public required string Nickname { get; set; }

        [Required]
        public required string[] Hobbies { get; set; }

        [Required]
        public required DateTime Birthday { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }

        [Required]
        [Phone]
        public required string PhoneNumber { get; set; }

    }
}