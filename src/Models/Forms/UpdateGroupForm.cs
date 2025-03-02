using System;

namespace Herta.Models.Forms.UpdateGroupForm;

public sealed class UpdateGroupForm
{
    public int GroupId { get; set; }
    public int OwnerId { get; set; }
    public Dictionary<string, string>? Fields { get; set; }
}