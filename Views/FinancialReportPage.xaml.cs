using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Windows.Storage.Pickers;
using WinRT.Interop;

namespace OpticaPro.Views
{
    public class ReportRow
    {
        public string Fecha { get; set; }
        public string Detalle { get; set; }
        public string Tipo { get; set; }
        public decimal Ingreso { get; set; }
        public decimal Gasto { get; set; }
        public decimal Neto => Ingreso - Gasto;

        public string IngresoDisplay => Ingreso > 0 ? Ingreso.ToString("C2", CultureInfo.GetCultureInfo("en-US")) : "-";
        public string GastoDisplay => Gasto > 0 ? Gasto.ToString("C2", CultureInfo.GetCultureInfo("en-US")) : "-";
        public string NetoDisplay => Neto.ToString("C2", CultureInfo.GetCultureInfo("en-US"));
    }

    public sealed partial class FinancialReportPage : Page
    {
        private List<ReportRow> _currentReportData = new List<ReportRow>();
        private decimal _currentIngresos = 0;
        private decimal _currentGastos = 0;
        private decimal _currentUtilidad = 0;

        public FinancialReportPage()
        {
            this.InitializeComponent();
            if (PickerFecha != null)
            {
                PickerFecha.Date = DateTimeOffset.Now;
            }
        }

        private void CmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e) => CalcularReporte();
        private void PickerFecha_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) => CalcularReporte();
        private void Generate_Click(object sender, RoutedEventArgs e) => CalcularReporte();

        // --- LÓGICA DE IMPRESIÓN PDF CORREGIDA ---
        private async void PrintPdf_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CalcularReporte();

                if (!_currentReportData.Any())
                {
                    ShowErrorDialog("No hay datos para generar el reporte.");
                    return;
                }

                // 1. OBTENER VENTANA PRINCIPAL DE FORMA SEGURA
                // Si esto es nulo, el FileSavePicker crashea la app
                if (App.m_window == null)
                {
                    ShowErrorDialog("Error Interno: No se detectó la ventana principal. Reinicia la aplicación.");
                    return;
                }

                // 2. Configurar el guardado
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Documento PDF", new List<string>() { ".pdf" });
                savePicker.SuggestedFileName = $"Reporte_Financiero_{DateTime.Now:yyyyMMdd_HHmm}";

                // 3. Inicializar el Picker con el Handle de la ventana ACTUALIZADA
                IntPtr hwnd = WindowNative.GetWindowHandle(App.m_window);
                InitializeWithWindow.Initialize(savePicker, hwnd);

                // 4. Abrir diálogo
                var file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    // 5. Generar PDF
                    var printService = new PrintService();
                    string periodoNombre = (CmbPeriodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Reporte";
                    DateTime fechaReporte = PickerFecha.Date.Value.DateTime;

                    printService.GenerateFinancialReport(
                        file.Path,
                        periodoNombre,
                        fechaReporte,
                        _currentReportData,
                        _currentIngresos,
                        _currentGastos,
                        _currentUtilidad
                    );

                    ShowErrorDialog("Reporte guardado exitosamente.", "Éxito");
                }
            }
            catch (Exception ex)
            {
                ShowErrorDialog($"Ocurrió un error al generar el PDF:\n{ex.Message}");
            }
        }

        private async void ShowErrorDialog(string message, string title = "Atención")
        {
            if (this.XamlRoot != null)
            {
                ContentDialog dialog = new ContentDialog
                {
                    Title = title,
                    Content = message,
                    CloseButtonText = "Entendido",
                    XamlRoot = this.XamlRoot
                };
                await dialog.ShowAsync();
            }
        }

        private void CalcularReporte()
        {
            try
            {
                if (PickerFecha == null || CmbPeriodo == null) return;
                if (PickerFecha.Date == null) return;

                DateTime fechaBase = PickerFecha.Date.Value.DateTime;
                string tipoPeriodo = (CmbPeriodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Diario";

                var todosLosPedidos = PatientRepository.GetAllOrders() ?? new List<Order>();
                List<ReportRow> movimientos = new List<ReportRow>();

                foreach (var orden in todosLosPedidos)
                {
                    if (DateTime.TryParseExact(orden.Date, "dd/MM/yyyy", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime fechaOrden))
                    {
                        bool incluir = false;
                        switch (tipoPeriodo)
                        {
                            case "Diario":
                                incluir = (fechaOrden.Date == fechaBase.Date);
                                break;
                            case "Semanal":
                                var cal = CultureInfo.CurrentCulture.Calendar;
                                var day = cal.GetDayOfWeek(fechaBase);
                                int diff = day - DayOfWeek.Monday;
                                if (diff < 0) diff += 7;
                                DateTime inicioSemana = fechaBase.AddDays(-diff).Date;
                                DateTime finSemana = inicioSemana.AddDays(7).Date;
                                incluir = (fechaOrden >= inicioSemana && fechaOrden < finSemana);
                                break;
                            case "Mensual":
                                incluir = (fechaOrden.Month == fechaBase.Month && fechaOrden.Year == fechaBase.Year);
                                break;
                            case "Anual":
                                incluir = (fechaOrden.Year == fechaBase.Year);
                                break;
                        }

                        if (incluir)
                        {
                            string nombreCliente = !string.IsNullOrEmpty(orden.ClientName) ? orden.ClientName : "Cliente General";

                            if (orden.Deposit > 0)
                                movimientos.Add(new ReportRow { Fecha = orden.Date, Detalle = nombreCliente, Tipo = $"Venta: {orden.FrameModel ?? "Lentes"}", Ingreso = orden.Deposit, Gasto = 0 });

                            if (orden.LabCost > 0)
                                movimientos.Add(new ReportRow { Fecha = orden.Date, Detalle = $"Lab: {orden.Laboratory ?? "Externo"}", Tipo = "Costo", Ingreso = 0, Gasto = orden.LabCost });
                        }
                    }
                }

                _currentReportData = movimientos.OrderByDescending(x => DateTime.ParseExact(x.Fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture)).ToList();
                _currentIngresos = _currentReportData.Sum(x => x.Ingreso);
                _currentGastos = _currentReportData.Sum(x => x.Gasto);
                _currentUtilidad = _currentIngresos - _currentGastos;

                ReportList.ItemsSource = _currentReportData;
                var usCulture = CultureInfo.GetCultureInfo("en-US");
                TxtIngresos.Text = _currentIngresos.ToString("C2", usCulture);
                TxtGastos.Text = _currentGastos.ToString("C2", usCulture);
                TxtUtilidad.Text = _currentUtilidad.ToString("C2", usCulture);

                if (_currentUtilidad >= 0) TxtUtilidad.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
                else TxtUtilidad.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);

                EmptyReportState.Visibility = _currentReportData.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error al calcular reporte: {ex.Message}");
            }
        }
    }
}