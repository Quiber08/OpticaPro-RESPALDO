using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OpticaPro.Models
{
    [Table("messages")]
    public class MessageModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("license_key")]
        public string LicenseKey { get; set; }

        [Column("content")]
        public string Content { get; set; }

        [Column("is_admin")]
        public bool IsAdmin { get; set; }

        [Column("is_read")]
        public bool IsRead { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}