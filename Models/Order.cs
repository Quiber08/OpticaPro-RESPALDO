using SQLite;
using System;

namespace OpticaPro.Models
{
    [Table("Order")] // Forzamos el nombre de la tabla para evitar confusiones plurales/singulares
    public class Order
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string PatientId { get; set; } // Vinculación con el paciente (como String)

        public string Date { get; set; }
        public string DeliveryDate { get; set; }

        // Detalles del trabajo
        public string FrameModel { get; set; }
        public string LensType { get; set; }
        public string Laboratory { get; set; }

        // Estado
        public string Status { get; set; } // "Pendiente", "Entregado", "Plan Acumulativo"

        // Financiero
        public decimal TotalAmount { get; set; }
        public decimal Deposit { get; set; }
        public decimal Balance { get; set; }
        public decimal LabCost { get; set; }
        public decimal Profit { get; set; }

        // Datos redundantes para facilitar reportes sin joins complejos
        public string ClientName { get; set; }
        public string LinkedExamSummary { get; set; }
        public string Notes { get; set; }
    }
}