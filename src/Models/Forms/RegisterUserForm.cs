using System.ComponentModel.DataAnnotations;

namespace Herta.Models.Forms.RegisterUserForm
{
    public class RegisterUserForm
    {
        [Required]
        [StringLength(50)]
        public required string Username { get; set; }

        [Required]
        [StringLength(100)]
        public required string Password { get; set; }

        [Required]
        [EmailAddress]
        public required string Email { get; set; }
    }
}