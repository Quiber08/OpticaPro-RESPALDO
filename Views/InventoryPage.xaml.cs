using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.Linq;

namespace OpticaPro.Views
{
    public sealed partial class InventoryPage : Page
    {
        private List<Product> _allProducts;

        public InventoryPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadData();
        }

        private void LoadData()
        {
            _allProducts = InventoryRepository.GetAllProducts() ?? new List<Product>();
            ApplyFilters();
            UpdateKPIs();
        }

        private void UpdateKPIs()
        {
            if (_allProducts == null) return;
            if (TxtTotalItems != null) TxtTotalItems.Text = _allProducts.Count.ToString();
            if (TxtLowStock != null) TxtLowStock.Text = _allProducts.Count(p => p.Stock <= 5).ToString();
        }

        private void ApplyFilters()
        {
            if (_allProducts == null) return;

            var query = SearchBox.Text?.ToLower() ?? "";
            var category = (CmbCategoryFilter.SelectedItem as ComboBoxItem)?.Content?.ToString();

            var filtered = _allProducts.Where(p =>
            {
                var pName = p.Name?.ToLower() ?? "";
                var pCode = p.Code?.ToLower() ?? "";
                var pBrand = p.Brand?.ToLower() ?? "";
                bool matchesSearch = pName.Contains(query) || pCode.Contains(query) || pBrand.Contains(query);
                bool matchesCategory = category == null || category == "Todos" || category == "Categoría" || p.Category == category;
                return matchesSearch && matchesCategory;
            }).ToList();

            if (InventoryList != null) InventoryList.ItemsSource = filtered;
            if (EmptyState != null) EmptyState.Visibility = filtered.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args) => ApplyFilters();
        private void CmbCategoryFilter_SelectionChanged(object sender, SelectionChangedEventArgs e) => ApplyFilters();

        // --- AQUI ESTA EL CAMBIO: NUEVA VENTANA ---
        private void AddProduct_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProductWindow(null); // Pasamos null para crear nuevo
            window.Activate();
            window.Closed += (s, args) => LoadData(); // Recargar lista al cerrar ventana
        }

        private void EditProduct_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var product = btn?.Tag as Product;
            if (product != null)
            {
                var window = new ProductWindow(product); // Pasamos el producto a editar
                window.Activate();
                window.Closed += (s, args) => LoadData(); // Recargar lista al cerrar ventana
            }
        }
        // ------------------------------------------

        private async void DeleteProduct_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var product = btn?.Tag as Product;
            if (product != null)
            {
                ContentDialog deleteDialog = new ContentDialog
                {
                    Title = "Eliminar Producto",
                    Content = $"¿Eliminar '{product.Name}' del inventario?",
                    PrimaryButtonText = "Eliminar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                if (await deleteDialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    InventoryRepository.DeleteProduct(product);
                    LoadData();
                }
            }
        }

        private void BulkAdd_Click(object sender, RoutedEventArgs e)
        {
            var bulkWindow = new BulkInventoryWindow();
            bulkWindow.Activate();
            bulkWindow.Closed += (s, args) => { LoadData(); };
        }
    }
}