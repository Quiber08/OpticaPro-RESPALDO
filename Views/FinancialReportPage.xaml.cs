using Microsoft.UI; // Importante para los colores
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media; // Para SolidColorBrush
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

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
        public FinancialReportPage()
        {
            this.InitializeComponent();
            // Aseguramos que el PickerFecha tenga un valor inicial seguro
            if (PickerFecha != null)
            {
                PickerFecha.Date = DateTimeOffset.Now;
            }
        }

        private void CmbPeriodo_SelectionChanged(object sender, SelectionChangedEventArgs e) => CalcularReporte();
        private void PickerFecha_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) => CalcularReporte();
        private void Generate_Click(object sender, RoutedEventArgs e) => CalcularReporte();

        private void CalcularReporte()
        {
            // --- CORRECCIÓN CRÍTICA PARA EVITAR CRASH ---
            // Si la página se está cargando, estos controles pueden ser nulos.
            // Esta verificación evita que la app se cierre.
            if (PickerFecha == null || CmbPeriodo == null) return;

            // 1. Obtener parámetros
            if (PickerFecha.Date == null) return;
            DateTime fechaBase = PickerFecha.Date.Value.DateTime;
            string tipoPeriodo = (CmbPeriodo.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Diario";

            // 2. Obtener datos
            var pacientes = PatientRepository.GetAllPatients();
            List<ReportRow> movimientos = new List<ReportRow>();

            // 3. Procesar datos
            foreach (var p in pacientes)
            {
                if (p.OrderHistory != null)
                {
                    foreach (var orden in p.OrderHistory)
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
                                if (orden.Deposit > 0)
                                {
                                    movimientos.Add(new ReportRow
                                    {
                                        Fecha = orden.Date,
                                        Detalle = p.FullName,
                                        Tipo = $"Venta: {orden.FrameModel}",
                                        Ingreso = orden.Deposit,
                                        Gasto = 0
                                    });
                                }

                                if (orden.LabCost > 0)
                                {
                                    movimientos.Add(new ReportRow
                                    {
                                        Fecha = orden.Date,
                                        Detalle = $"Lab: {orden.Laboratory}",
                                        Tipo = "Costo de Producción",
                                        Ingreso = 0,
                                        Gasto = orden.LabCost
                                    });
                                }
                            }
                        }
                    }
                }
            }

            // 4. Mostrar Resultados (Ordenados por fecha usando ParseExact seguro)
            var listaOrdenada = movimientos
                .OrderByDescending(x => DateTime.ParseExact(x.Fecha, "dd/MM/yyyy", CultureInfo.InvariantCulture))
                .ToList();

            ReportList.ItemsSource = listaOrdenada;

            decimal totalIngresos = listaOrdenada.Sum(x => x.Ingreso);
            decimal totalGastos = listaOrdenada.Sum(x => x.Gasto);
            decimal utilidad = totalIngresos - totalGastos;

            var usCulture = CultureInfo.GetCultureInfo("en-US");
            TxtIngresos.Text = totalIngresos.ToString("C2", usCulture);
            TxtGastos.Text = totalGastos.ToString("C2", usCulture);
            TxtUtilidad.Text = utilidad.ToString("C2", usCulture);

            // --- CORRECCIÓN DE COLORES ---
            // Usamos Microsoft.UI.Colors en lugar de Windows.UI.Colors
            if (utilidad >= 0)
                TxtUtilidad.Foreground = new SolidColorBrush(Microsoft.UI.Colors.DodgerBlue);
            else
                TxtUtilidad.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);

            EmptyReportState.Visibility = listaOrdenada.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }
    }
}