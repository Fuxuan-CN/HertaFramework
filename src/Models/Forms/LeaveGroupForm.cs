

namespace Herta.Models.Forms.LeaveGroupForm;

public sealed class LeaveGroupForm
{
    public int GroupId { get; set; }
    public int UserId { get; set; }
    public string? Reason { get; set; }
}