using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;
using System.Threading.Tasks;

namespace OpticaPro.Views
{
    public sealed partial class DashboardPage : Page
    {
        public DashboardPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            try
            {
                // 1. Mensaje de Bienvenida Seguro
                if (TxtWelcome != null)
                {
                    string fechaHoy = DateTime.Now.ToString("D", new CultureInfo("es-ES"));
                    if (fechaHoy.Length > 0) fechaHoy = char.ToUpper(fechaHoy[0]) + fechaHoy.Substring(1);
                    TxtWelcome.Text = $"Bienvenido al Sistema OpticaPro  •  {fechaHoy}";
                }

                // 2. Cargar estadísticas con protección
                CalculateRealStats();
            }
            catch (Exception ex)
            {
                // SI ALGO FALLA, NO SE CIERRA: Muestra el error
                await ShowErrorDialog($"Error al cargar el Dashboard: {ex.Message}");
            }
        }

        private void CalculateRealStats()
        {
            try
            {
                // 1. Obtener datos
                // Necesitamos los pacientes para la lista de "Recientes" y el contador de visitas de hoy
                var allPatients = PatientRepository.GetAllPatients() ?? new List<Patient>();

                // CORRECCIÓN: Obtenemos los pedidos DIRECTAMENTE de la BD para los cálculos financieros
                var allOrders = PatientRepository.GetAllOrders() ?? new List<Order>();

                var allProducts = InventoryRepository.GetAllProducts() ?? new List<Product>();

                string today = DateTime.Now.ToString("dd/MM/yyyy");

                // --- ESTADÍSTICA 1: PACIENTES HOY ---
                int patientsToday = allPatients.Count(p => p.LastVisit == today);
                if (TxtPatientsCount != null) TxtPatientsCount.Text = patientsToday.ToString();

                // --- ESTADÍSTICA 2: FINANZAS ---
                decimal totalIngresos = 0;
                decimal totalGastos = 0;
                int pendingOrders = 0;

                // A. Sumar desde LA LISTA GENERAL DE PEDIDOS (Ya no desde pacientes individuales)
                foreach (var o in allOrders)
                {
                    // Contar pendientes
                    if (o.Status != "Entregado") pendingOrders++;

                    // Sumar finanzas
                    totalIngresos += o.Deposit;
                    totalGastos += o.LabCost;
                }

                // B. Sumar gastos desde INVENTARIO (Costo de stock actual)
                foreach (var prod in allProducts)
                {
                    totalGastos += (prod.PurchasePrice * prod.Stock);
                }

                // C. GANANCIA NETA
                decimal gananciaNeta = totalIngresos - totalGastos;
                var usCulture = CultureInfo.GetCultureInfo("en-US");

                // Actualizar UI (Verificando nulos)
                if (TxtIncome != null) TxtIncome.Text = totalIngresos.ToString("C2", usCulture);
                if (TxtPrescriptions != null) TxtPrescriptions.Text = pendingOrders.ToString();
                if (TxtBalanceIncome != null) TxtBalanceIncome.Text = totalIngresos.ToString("C2", usCulture);
                if (TxtExpenses != null) TxtExpenses.Text = totalGastos.ToString("C2", usCulture);

                if (TxtNetProfit != null)
                {
                    TxtNetProfit.Text = gananciaNeta.ToString("C2", usCulture);
                    // Lógica de color: Rojo si es negativo, Azul si es positivo
                    if (gananciaNeta < 0)
                        TxtNetProfit.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
                    else
                        TxtNetProfit.Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Windows.UI.Color.FromArgb(255, 33, 150, 243));
                }

                // --- ESTADÍSTICA 3: PACIENTES RECIENTES ---
                // Esto se mantiene igual porque 'LastVisit' sí se carga en el modelo Patient
                var recentPatients = allPatients
                    .OrderByDescending(p => DateTime.TryParse(p.LastVisit, out DateTime d) ? d : DateTime.MinValue)
                    .Take(5)
                    .ToList();

                if (RecentPatientsList != null) RecentPatientsList.ItemsSource = recentPatients;
                if (EmptyRecentState != null) EmptyRecentState.Visibility = recentPatients.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                // Loguear error en consola de depuración si es necesario
                System.Diagnostics.Debug.WriteLine($"Error calculando estadísticas: {ex.Message}");
            }
        }

        private async Task ShowErrorDialog(string message)
        {
            ContentDialog dialog = new ContentDialog
            {
                Title = "Aviso",
                Content = message,
                CloseButtonText = "Ok",
                XamlRoot = this.Content?.XamlRoot
            };
            if (dialog.XamlRoot != null) await dialog.ShowAsync();
        }

        private void NewPatient_Click(object sender, RoutedEventArgs e) => this.Frame.Navigate(typeof(AddPatientPage));
        private void NewConsult_Click(object sender, RoutedEventArgs e) => this.Frame.Navigate(typeof(PatientsPage));
    }
}