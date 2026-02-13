using SQLite;
using System.Collections.Generic;
using Microsoft.UI.Xaml.Media; // Necesario para que DashboardPage no falle
using Windows.UI;

namespace OpticaPro.Models
{
    public class Patient
    {
        [PrimaryKey]
        public string Id { get; set; }

        public string FullName { get; set; }
        public string Initials { get; set; }
        public string Dni { get; set; }
        public int Age { get; set; }
        public string Occupation { get; set; }
        public string Phone { get; set; }
        public string Email { get; set; }
        public string Address { get; set; }
        public string City { get; set; }
        public string LastVisit { get; set; }

        // Estado: "Al día", "Deuda", etc.
        public string Status { get; set; }

        public string VisualHistory { get; set; }

        // Listas ignoradas por la Base de Datos
        [Ignore]
        public List<ClinicalExam> ExamHistory { get; set; } = new List<ClinicalExam>();

        [Ignore]
        public List<Order> OrderHistory { get; set; } = new List<Order>();

        // ==============================================================================
        // PROPIEDADES VISUALES (Restauradas para compatibilidad con DashboardPage)
        // ==============================================================================
        // Nota: PatientsPage usará su propia lógica segura, pero dejamos esto aquí
        // para que el resto de la app no se rompa.

        [Ignore]
        public SolidColorBrush StatusBackgroundBrush
        {
            get
            {
                if (Status == "Deuda")
                    return new SolidColorBrush(Color.FromArgb(255, 253, 236, 236)); // Rojo suave
                return new SolidColorBrush(Color.FromArgb(255, 237, 247, 237));     // Verde suave
            }
        }

        [Ignore]
        public SolidColorBrush StatusTextBrush
        {
            get
            {
                if (Status == "Deuda")
                    return new SolidColorBrush(Color.FromArgb(255, 198, 40, 40));   // Rojo fuerte
                return new SolidColorBrush(Color.FromArgb(255, 30, 70, 32));        // Verde fuerte
            }
        }
    }
}