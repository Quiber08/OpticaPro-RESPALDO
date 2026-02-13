using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using OpticaPro.Models;
using OpticaPro.Services;

namespace OpticaPro.Views
{
    public sealed partial class PrescriptionsPage : Page
    {
        private List<OrderDisplayItem> _allItems = new List<OrderDisplayItem>();

        public PrescriptionsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Ya no inicializamos impresión aquí, porque lo hará la ventana nueva.
            LoadRealData();
        }

        private void LoadRealData()
        {
            try
            {
                _allItems.Clear();

                // 1. Obtener todos los pacientes
                var patients = PatientRepository.GetAllPatients();

                foreach (var p in patients)
                {
                    // 2. Obtener pedidos de forma segura
                    List<Order> dbOrders = null;
                    try
                    {
                        dbOrders = PatientRepository.GetOrdersByPatientId(p.Id);
                    }
                    catch
                    {
                        dbOrders = new List<Order>();
                    }

                    // 3. Crear los ítems para la lista
                    if (dbOrders != null && dbOrders.Count > 0)
                    {
                        foreach (var order in dbOrders)
                            _allItems.Add(new OrderDisplayItem { PatientOwner = p, OrderData = order });
                    }
                    else
                    {
                        _allItems.Add(new OrderDisplayItem { PatientOwner = p, OrderData = null });
                    }
                }

                // 4. Ordenar por fecha (más reciente primero)
                _allItems = _allItems.OrderByDescending(o =>
                    o.OrderData != null && DateTime.TryParse(o.Date, out DateTime d) ? d : DateTime.MinValue
                ).ToList();

                UpdateList(_allItems);
            }
            catch (Exception ex)
            {
                // Mensaje de seguridad por si falla la base de datos
                var dialog = new ContentDialog
                {
                    Title = "Atención",
                    Content = $"Hubo un problema al cargar los datos: {ex.Message}",
                    CloseButtonText = "Entendido",
                    XamlRoot = this.Content?.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }

        private void UpdateList(List<OrderDisplayItem> list)
        {
            if (PrescriptionsList == null) return;
            PrescriptionsList.ItemsSource = list;

            if (EmptyState != null)
                EmptyState.Visibility = list.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                var query = sender.Text.ToLower();
                var filtered = _allItems.Where(o =>
                    (o.PatientName != null && o.PatientName.ToLower().Contains(query)) ||
                    (o.Frame != null && o.Frame.ToLower().Contains(query))
                ).ToList();
                UpdateList(filtered);
            }
            catch { }
        }

        // --- ESTE ES EL NUEVO MÉTODO SIMPLIFICADO ---
        private void PrintFullHistory_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var item = btn?.Tag as OrderDisplayItem;

            if (item == null || item.PatientOwner == null) return;

            // Abrimos la nueva ventana independiente.
            // Esto soluciona de raíz el problema del PDF en blanco o errores de impresión.
            var historyWindow = new HistoryPreviewWindow(item.PatientOwner);
            historyWindow.Activate();
        }
    }
}