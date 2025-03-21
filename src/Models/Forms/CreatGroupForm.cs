

namespace Herta.Models.Forms.CreateGroupForm;

public sealed class CreateGroupForm
{
    public required int OwnerId { get; set; }
    public required string GroupName { get; set; }
    public required string Description { get; set; }
    public string? AvatarUrl { get; set; }
}
