using System;
using System.ComponentModel.DataAnnotations;

namespace Herta.Models.DataModels.User
{
    public class User
    {
        [Key]
        public int Id;

        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        [StringLength(100)]
        public required string PasswordHash { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}