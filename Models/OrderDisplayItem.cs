using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System.Globalization;

namespace OpticaPro.Models
{
    public class OrderDisplayItem
    {
        public Patient PatientOwner { get; set; }
        public Order OrderData { get; set; } // Puede ser null

        public string PatientName => PatientOwner?.FullName ?? "Desconocido";

        // --- CORRECCIÓN ANTI-CRASH: Manejo seguro de nulos ---
        public string Date => OrderData?.Date ?? "---";

        public string DeliveryDate => OrderData?.DeliveryDate ?? "---";

        public string Frame => OrderData?.FrameModel ?? "Ficha Médica / Informe";

        public string Status => OrderData?.Status ?? "Expediente";

        public decimal Balance => OrderData?.Balance ?? 0;

        public string FormattedBalance => (OrderData?.Balance ?? 0).ToString("C2", CultureInfo.GetCultureInfo("en-US"));

        public SolidColorBrush StatusColorBrush
        {
            get
            {
                if (OrderData == null)
                {
                    // Color diferente para indicar que es solo historial
                    return new SolidColorBrush(Colors.CornflowerBlue);
                }

                return Status == "Entregado"
                    ? new SolidColorBrush(Colors.Green)
                    : new SolidColorBrush(Colors.DarkOrange);
            }
        }
    }
}