using Supabase.Postgrest.Attributes;
using Supabase.Postgrest.Models;
using System;

namespace OpticaPro.Models
{
    // 1. Conectamos con la tabla CORRECTA que creaste en SQL
    [Table("app_updates_windows")]
    public class UpdateModel : BaseModel
    {
        [PrimaryKey("id", false)]
        public string Id { get; set; }

        [Column("version")]
        public string Version { get; set; }

        // 2. SOLUCIÓN DEL BUG: Mapeamos la columna 'notes' de SQL a 'Notes' de C#
        // Así tu SettingsPage ya no dará error porque ahora sí existe .Notes
        [Column("notes")]
        public string Notes { get; set; }

        [Column("download_url")]
        public string DownloadUrl { get; set; }

        [Column("mandatory")]
        public bool Mandatory { get; set; }

        [Column("created_at")]
        public DateTime CreatedAt { get; set; }
    }
}