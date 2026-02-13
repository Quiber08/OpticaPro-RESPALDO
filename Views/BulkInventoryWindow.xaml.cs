using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using OpticaPro.Models;
using OpticaPro.Services;
using System;

namespace OpticaPro.Views
{
    public class BulkProduct
    {
        public string Code { get; set; } = "";
        public string Name { get; set; } = "";
        public int CategoryIndex { get; set; } = 0;
        public string Price { get; set; } = "";

        // --- CAMPO NUEVO ---
        public string Cost { get; set; } = "";

        public string Stock { get; set; } = "";
    }

    public sealed partial class BulkInventoryWindow : Window
    {
        public ObservableCollection<BulkProduct> ProductsList { get; set; } = new ObservableCollection<BulkProduct>();

        public BulkInventoryWindow()
        {
            this.InitializeComponent();
            this.SystemBackdrop = new Microsoft.UI.Xaml.Media.MicaBackdrop();
            for (int i = 0; i < 5; i++) ProductsList.Add(new BulkProduct());
            BulkList.ItemsSource = ProductsList;
        }

        private void AddRow_Click(object sender, RoutedEventArgs e) => ProductsList.Add(new BulkProduct());

        private void RemoveRow_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is BulkProduct item) ProductsList.Remove(item);
        }

        private async void SaveAll_Click(object sender, RoutedEventArgs e)
        {
            int guardados = 0;

            foreach (var item in ProductsList)
            {
                if (!string.IsNullOrWhiteSpace(item.Name) && !string.IsNullOrWhiteSpace(item.Code))
                {
                    decimal.TryParse(item.Price, out decimal price);

                    // LEER COSTO
                    decimal.TryParse(item.Cost, out decimal cost);

                    int.TryParse(item.Stock, out int stock);

                    string categoria = "Armazones";
                    if (item.CategoryIndex == 1) categoria = "Lunas";
                    else if (item.CategoryIndex == 2) categoria = "Accesorios";
                    else if (item.CategoryIndex == 3) categoria = "Lentes de Contacto";

                    var newProduct = new Product
                    {
                        Code = item.Code,
                        Name = item.Name,
                        Category = categoria,
                        Brand = "Genérico",
                        Price = price,
                        PurchasePrice = cost, // GUARDAR COSTO
                        Stock = stock
                    };

                    InventoryRepository.AddProduct(newProduct);
                    guardados++;
                }
            }

            ContentDialog dialog = new ContentDialog
            {
                Title = "Proceso Completado",
                Content = $"Se han ingresado {guardados} productos al inventario.",
                CloseButtonText = "Cerrar Ventana",
                XamlRoot = this.Content.XamlRoot
            };

            await dialog.ShowAsync();
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}