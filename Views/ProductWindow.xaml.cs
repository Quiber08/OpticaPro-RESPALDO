using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpticaPro.Models;
using OpticaPro.Services;
using System;

namespace OpticaPro.Views
{
    public sealed partial class ProductWindow : Window
    {
        private Product _productToEdit;

        public ProductWindow(Product product = null)
        {
            this.InitializeComponent();
            _productToEdit = product;

            // Ajustar tamaño de ventana
            this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1050, 600));

            LoadData();
        }

        private void LoadData()
        {
            if (_productToEdit != null)
            {
                TitleText.Text = $"Editar: {_productToEdit.Name}";
                TxtCode.Text = _productToEdit.Code;
                TxtName.Text = _productToEdit.Name;
                TxtBrand.Text = _productToEdit.Brand;

                // Seleccionar categoría
                foreach (ComboBoxItem item in CmbCategory.Items)
                {
                    if (item.Content.ToString() == _productToEdit.Category)
                    {
                        CmbCategory.SelectedItem = item;
                        break;
                    }
                }

                NbCost.Value = (double)_productToEdit.PurchasePrice;
                NbPrice.Value = (double)_productToEdit.Price;
                NbStock.Value = _productToEdit.Stock;
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            // Validaciones
            if (string.IsNullOrWhiteSpace(TxtCode.Text) || string.IsNullOrWhiteSpace(TxtName.Text))
            {
                ShowErrorDialog("El Código y el Nombre son obligatorios.");
                return;
            }

            string category = (CmbCategory.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Armazones";

            if (_productToEdit == null)
            {
                // MODO NUEVO
                var newP = new Product
                {
                    Code = TxtCode.Text,
                    Name = TxtName.Text,
                    Brand = TxtBrand.Text,
                    Category = category,
                    PurchasePrice = (decimal)NbCost.Value,
                    Price = (decimal)NbPrice.Value,
                    Stock = (int)NbStock.Value
                };
                InventoryRepository.AddProduct(newP);
            }
            else
            {
                // MODO EDITAR
                _productToEdit.Code = TxtCode.Text;
                _productToEdit.Name = TxtName.Text;
                _productToEdit.Brand = TxtBrand.Text;
                _productToEdit.Category = category;
                _productToEdit.PurchasePrice = (decimal)NbCost.Value;
                _productToEdit.Price = (decimal)NbPrice.Value;
                _productToEdit.Stock = (int)NbStock.Value;
                InventoryRepository.UpdateProduct(_productToEdit);
            }

            this.Close(); // Cierra la ventana al terminar
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void ShowErrorDialog(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Atención",
                Content = message,
                CloseButtonText = "Ok",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}