using System;
using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;

namespace OpticaPro.Models
{
    [Table("license_activations")] // Nombre exacto de tu SQL
    public class ActivationModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("license_key")]
        public string LicenseKey { get; set; }

        [Column("hardware_id")]
        public string HardwareId { get; set; }

        [Column("device_name")]
        public string DeviceName { get; set; }

        [Column("activated_at")]
        public DateTime ActivatedAt { get; set; }
    }
}