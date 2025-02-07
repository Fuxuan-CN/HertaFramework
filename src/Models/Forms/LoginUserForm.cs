
using System.ComponentModel.DataAnnotations;

namespace Herta.Models.Forms.LoginUserForm
{
    public sealed class LoginUserForm
    {
        [Required(ErrorMessage = "用户名不能为空")]
        [StringLength(50, ErrorMessage = "用户名的长度必须在1到50个字符之间")]
        public required string Username { get; set; }
        [Required(ErrorMessage = "密码不能为空")]
        [StringLength(255, ErrorMessage = "密码的长度必须在1到255个字符之间")]
        public required string Password { get; set; }
    }
}
