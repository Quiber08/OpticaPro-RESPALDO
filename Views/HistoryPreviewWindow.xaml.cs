using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Text;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Linq;
// NOTA IMPORTANTE: NO AGREGUES 'using Windows.UI;' AQUI. CAUSA CONFLICTO.

namespace OpticaPro.Views
{
    public sealed partial class HistoryPreviewWindow : Window
    {
        private Patient _patient;

        public HistoryPreviewWindow(Patient patient)
        {
            this.InitializeComponent();
            _patient = patient;

            // Ajustar tamaño de ventana
            try { this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1000, 900)); } catch { }

            LoadData();
        }

        private void LoadData()
        {
            if (_patient == null) return;

            // 1. Cargar configuración de la clínica
            var s = SettingsService.Current;
            RunClinicName.Text = !string.IsNullOrEmpty(s.ClinicName) ? s.ClinicName.ToUpper() : "OPTICA PRO";
            RunClinicAddress.Text = s.ClinicAddress;
            RunClinicPhone.Text = s.ClinicPhone;
            RunDate.Text = DateTime.Now.ToString("dd/MM/yyyy HH:mm");

            // 2. Datos Paciente
            RunPatientName.Text = _patient.FullName.ToUpper();
            RunPatientAge.Text = $"{_patient.Age} años";
            RunPatientDni.Text = _patient.Dni;
            RunPatientPhone.Text = _patient.Phone;

            // 3. Cargar Historial Médico
            var exams = PatientRepository.GetExamsByPatientId(_patient.Id);
            if (exams != null && exams.Count > 0)
            {
                foreach (var exam in exams)
                {
                    PanelExams.Children.Add(CreateHistoryItem($"📅 {exam.Date}",
                        $"Dx: {exam.DiagnosticoResumen}\nObservaciones: {exam.Observaciones}"));
                }
            }
            else
            {
                PanelExams.Children.Add(CreateEmptyItem("No hay registros clínicos."));
            }

            // 4. Cargar Historial Pedidos
            var orders = PatientRepository.GetOrdersByPatientId(_patient.Id);
            if (orders != null && orders.Count > 0)
            {
                foreach (var order in orders)
                {
                    var estado = string.IsNullOrEmpty(order.Status) ? "Registrado" : order.Status;
                    PanelOrders.Children.Add(CreateHistoryItem($"👓 {order.Date}",
                        $"Montura: {order.FrameModel}\nValor: {order.TotalAmount:C2} - Estado: {estado}"));
                }
            }
            else
            {
                PanelOrders.Children.Add(CreateEmptyItem("No hay pedidos registrados."));
            }
        }

        // Helper para crear filas visuales
        private Border CreateHistoryItem(string title, string content)
        {
            var stack = new StackPanel { Spacing = 4 };

            // Usamos Microsoft.UI.Colors explícitamente
            stack.Children.Add(new TextBlock
            {
                Text = title,
                FontWeight = FontWeights.Bold,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Black)
            });

            stack.Children.Add(new TextBlock
            {
                Text = content,
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.DarkSlateGray)
            });

            // Usamos Windows.UI.Color SOLO donde es estrictamente necesario y con nombre completo
            return new Border
            {
                BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(50, 0, 0, 0)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(0, 8, 0, 8),
                Child = stack
            };
        }

        private TextBlock CreateEmptyItem(string text)
        {
            return new TextBlock
            {
                Text = text,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                Margin = new Thickness(0, 10, 0, 10)
            };
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var printService = new PrintService(hWnd);

            try
            {
                await printService.PrintAsync(PrintArea);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}