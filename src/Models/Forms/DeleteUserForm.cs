

namespace Herta.Models.Forms.DeleteUserForm
{
    public sealed class DeleteUserForm
    {
        public required string Username { get; set; }
        public required string Reason { get; set; }
        public required string Password { get; set; }
    }
}