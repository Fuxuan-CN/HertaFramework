

namespace Herta.Models.Forms.CreatGroupForm
{
    public sealed class CreateGroupForm
    {
        public required int WhatUserCreatedId { get; set; }
        public required string GroupName { get; set; }
        public required string Description { get; set; }
        public string? AvatarUrl { get; set; }
    }
}