using System;
using Microsoft.UI;
using Microsoft.UI.Xaml.Media;
using System.Globalization;
using SQLite; // <--- NECESARIO

namespace OpticaPro.Models
{
    public class Product
    {
        [PrimaryKey] // <--- CLAVE ÚNICA
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public string Name { get; set; }
        public string Code { get; set; }
        public string Brand { get; set; }
        public string Category { get; set; }

        public decimal Price { get; set; }
        public decimal PurchasePrice { get; set; }

        public int Stock { get; set; }

        // PROPIEDADES CALCULADAS (NO GUARDAR EN BASE DE DATOS)

        [Ignore]
        public string FormattedPrice => Price.ToString("C2", CultureInfo.GetCultureInfo("en-US"));

        [Ignore]
        public string StockStatus => Stock <= 5 ? "Stock Bajo" : "En Stock";

        [Ignore] // IMPORTANTE: Si intentas guardar esto, la app explota
        public SolidColorBrush StockColor
        {
            get
            {
                if (Stock == 0) return new SolidColorBrush(Colors.Red);
                if (Stock <= 5) return new SolidColorBrush(Colors.Orange);
                return new SolidColorBrush(Colors.Green);
            }
        }
    }
}