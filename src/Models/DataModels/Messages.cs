
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Herta.Models.Enums.MessageType;

namespace Herta.Models.DataModels.Messages
{
    [Table("Messages")]
    public class Message
    {
        [Key]
        public int Id { get; set; }

        public int? UserId { get; set; }

        public string? Content { get; set; }

        [Required]
        public required MessageType Type { get; set; }
    }
}