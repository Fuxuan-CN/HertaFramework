
namespace Herta.Models.Forms.JoinGroupForm;

public sealed class JoinGroupForm
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string? JoinAbout { get; set; } // 加入的理由
}
