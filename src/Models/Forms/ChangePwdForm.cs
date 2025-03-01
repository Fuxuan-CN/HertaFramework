using System.ComponentModel.DataAnnotations;

namespace Herta.Models.Forms.ChangePasswordForm;

public sealed class ChangePasswordForm
{
    [Required]
    public required string Username { get; set; }

    [Required]
    [StringLength(100)]
    public required string OldPassword { get; set; }

    [Required]
    [StringLength(100)]
    public required string NewPassword { get; set; }
}