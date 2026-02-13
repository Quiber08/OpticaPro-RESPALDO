using SQLite;
using Microsoft.UI.Xaml.Media; // Necesario para Brush
using System.Text.Json.Serialization;

namespace OpticaPro.Models
{
    public class Appointment
    {
        [PrimaryKey]
        public string Id { get; set; }
        public string PatientId { get; set; }
        public string PatientName { get; set; }
        public string Date { get; set; }
        public string Time { get; set; }
        public string Reason { get; set; }

        // Estado: "Pendiente", "Completada", "Cancelada"
        public string Status { get; set; }

        // --- PROPIEDADES VISUALES (No se guardan en BD) ---
        // Estas propiedades son las que usa el XAML para pintar los colores

        [Ignore]
        [JsonIgnore]
        public Brush StatusColorBrush { get; set; }

        [Ignore]
        [JsonIgnore]
        public Brush StatusBackgroundBrush { get; set; }

        [Ignore]
        [JsonIgnore]
        public Brush StatusTextBrush { get; set; }
    }
}