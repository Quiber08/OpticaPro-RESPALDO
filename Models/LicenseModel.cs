using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OpticaPro.Models
{
    [Table("licenses")]
    public class LicenseModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("license_key")]
        public string LicenseKey { get; set; }

        [Column("client_name")]
        public string ClientName { get; set; }

        [Column("type")]
        public string Type { get; set; }

        [Column("status")]
        public string Status { get; set; }

        [Column("expiry_date")]
        public DateTime ExpiryDate { get; set; }

        [Column("max_devices")]
        public int MaxDevices { get; set; }

        [Column("is_active")]
        public bool IsActive { get; set; }
    }
}