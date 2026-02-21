using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Shapes;
using Microsoft.UI.Text;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Linq;
using System.Collections.Generic;
using Windows.Storage.Pickers; // Necesario para el selector de archivos

namespace OpticaPro.Views
{
    public sealed partial class HistoryPreviewWindow : Window
    {
        private Patient _patient;

        // --- PALETA DE COLORES PROFESIONAL ---
        private readonly SolidColorBrush _primaryColor = new SolidColorBrush(ColorHelper.FromArgb(255, 0, 51, 102));   // Azul Oscuro Institucional
        private readonly SolidColorBrush _headerBgColor = new SolidColorBrush(ColorHelper.FromArgb(255, 230, 242, 255)); // Azul muy pálido para cabeceras
        private readonly SolidColorBrush _sectionBgColor = new SolidColorBrush(ColorHelper.FromArgb(255, 248, 249, 250)); // Gris muy tenue para bloques
        private readonly SolidColorBrush _borderColor = new SolidColorBrush(ColorHelper.FromArgb(255, 220, 220, 220));   // Gris suave para bordes
        private readonly SolidColorBrush _labelColor = new SolidColorBrush(Microsoft.UI.Colors.DimGray);
        private readonly SolidColorBrush _valueColor = new SolidColorBrush(Microsoft.UI.Colors.Black);

        public HistoryPreviewWindow(Patient patient)
        {
            this.InitializeComponent();
            _patient = patient;

            try { this.AppWindow.Resize(new Windows.Graphics.SizeInt32(950, 900)); } catch { }

            LoadData();
        }

        private void LoadData()
        {
            if (_patient == null) return;

            // 1. Cabecera Clínica
            var s = SettingsService.Current;
            RunClinicName.Text = !string.IsNullOrEmpty(s.ClinicName) ? s.ClinicName.ToUpper() : "ÓPTICA PRO";
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
            PanelExams.Children.Clear();

            if (exams != null && exams.Count > 0)
            {
                foreach (var exam in exams)
                {
                    PanelExams.Children.Add(CreateExamCard(exam));
                }
            }
            else
            {
                PanelExams.Children.Add(CreateEmptyMessage("No hay registros clínicos disponibles."));
            }

            // 4. Cargar Historial Pedidos
            var orders = PatientRepository.GetOrdersByPatientId(_patient.Id);
            PanelOrders.Children.Clear();

            if (orders != null && orders.Count > 0)
            {
                foreach (var order in orders)
                {
                    PanelOrders.Children.Add(CreateOrderCard(order));
                }
            }
            else
            {
                PanelOrders.Children.Add(CreateEmptyMessage("No hay pedidos registrados."));
            }
        }

        // ==========================================
        //         DISEÑO DE TARJETA DE EXAMEN
        // ==========================================

        private Border CreateExamCard(ClinicalExam exam)
        {
            // Contenedor Principal (Tarjeta Blanca con Borde)
            var cardBorder = new Border
            {
                BorderBrush = _borderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(0, 0, 0, 20),
                Padding = new Thickness(0) // El padding lo manejan los hijos
            };

            var mainStack = new StackPanel();

            // --- 1. BARRA DE TÍTULO (FECHA) ---
            var titleBorder = new Border
            {
                Background = _headerBgColor,
                Padding = new Thickness(15, 8, 15, 8),
                CornerRadius = new CornerRadius(6, 6, 0, 0),
                BorderBrush = _borderColor,
                BorderThickness = new Thickness(0, 0, 0, 1)
            };
            var titleText = new TextBlock
            {
                Text = $"📅 CONSULTA DEL: {exam.Date}",
                FontSize = 14,
                FontWeight = FontWeights.Bold,
                Foreground = _primaryColor
            };
            titleBorder.Child = titleText;
            mainStack.Children.Add(titleBorder);

            // Contenedor de contenido interno
            var contentStack = new StackPanel { Padding = new Thickness(15), Spacing = 15 };

            // --- 2. BLOQUE: ANAMNESIS (Fondo Gris Tenue) ---
            var anamnesisStack = new StackPanel { Spacing = 4 };
            bool hasAnamnesis = false;

            if (!string.IsNullOrEmpty(exam.MotivoConsulta)) { anamnesisStack.Children.Add(CreateFieldRow("Motivo", exam.MotivoConsulta)); hasAnamnesis = true; }
            if (!string.IsNullOrEmpty(exam.HistoriaEnfermedad)) { anamnesisStack.Children.Add(CreateFieldRow("Historia", exam.HistoriaEnfermedad)); hasAnamnesis = true; }
            if (!string.IsNullOrEmpty(exam.MedicacionActual)) { anamnesisStack.Children.Add(CreateFieldRow("Medicación", exam.MedicacionActual)); hasAnamnesis = true; }

            string ant = "";
            if (!string.IsNullOrEmpty(exam.AntecedentesSistemicos)) ant += $"Sistémicos: {exam.AntecedentesSistemicos}. ";
            if (!string.IsNullOrEmpty(exam.AntecedentesOculares)) ant += $"Oculares: {exam.AntecedentesOculares}.";
            if (!string.IsNullOrEmpty(ant)) { anamnesisStack.Children.Add(CreateFieldRow("Antecedentes", ant)); hasAnamnesis = true; }

            if (hasAnamnesis)
            {
                contentStack.Children.Add(CreateSectionTitle("DATOS CLÍNICOS"));
                var box = CreateSectionBox(anamnesisStack);
                contentStack.Children.Add(box);
            }

            // --- 3. BLOQUE: REFRACTIVO ---
            var refracStack = new StackPanel { Spacing = 10 };

            // Lensometría
            if (!AreAllEmpty(exam.LensoSphOD, exam.LensoSphOI))
            {
                refracStack.Children.Add(CreateSubTitle("LENSOMETRÍA (Uso Anterior)"));
                refracStack.Children.Add(CreateRefractionTable(
                    exam.LensoSphOD, exam.LensoCylOD, exam.LensoAxisOD, exam.LensoAddOD,
                    exam.LensoSphOI, exam.LensoCylOI, exam.LensoAxisOI, exam.LensoAddOI
                ));
            }

            // Refracción Final
            refracStack.Children.Add(CreateSubTitle("REFRACCIÓN ACTUAL (RX FINAL)"));
            refracStack.Children.Add(CreateRefractionTable(
                exam.SphereOD, exam.CylOD, exam.AxisOD, exam.AddOD,
                exam.SphereOI, exam.CylOI, exam.AxisOI, exam.AddOI,
                exam.AvOD, exam.AvOI
            ));

            // Datos extra de refracción (DIP, K, etc.)
            string extras = "";
            if (!string.IsNullOrEmpty(exam.Dip)) extras += $"DIP: {exam.Dip} mm. ";
            if (!string.IsNullOrEmpty(exam.K1OD)) extras += $" | Queratometría: {exam.K1OD}/{exam.K2OD}x{exam.KAxisOD}";

            if (!string.IsNullOrEmpty(extras))
                refracStack.Children.Add(CreateFieldRow("Adicionales", extras));

            contentStack.Children.Add(CreateSectionTitle("ESTADO REFRACTIVO"));
            contentStack.Children.Add(CreateSectionBox(refracStack));

            // --- 4. BLOQUE: SALUD Y DIAGNÓSTICO ---
            var dxStack = new StackPanel { Spacing = 4 };

            // Salud Ocular
            string salud = "";
            if (exam.HasPterygiumOD || exam.HasPterygiumOI) salud += "Pterigión. ";
            if (exam.HasCataractOD || exam.HasCataractOI) salud += "Catarata. ";
            if (exam.HasGlaucomaSuspicionOD || exam.HasGlaucomaSuspicionOI) salud += "Sospecha Glaucoma. ";
            if (!string.IsNullOrEmpty(exam.FondoRetina)) salud += $"Fondo de Ojo: {exam.FondoRetina}";

            if (!string.IsNullOrEmpty(salud)) dxStack.Children.Add(CreateFieldRow("Salud Ocular", salud));

            // Diagnóstico
            if (!string.IsNullOrEmpty(exam.DiagnosticoResumen)) dxStack.Children.Add(CreateFieldRow("Diagnóstico", exam.DiagnosticoResumen));
            if (!string.IsNullOrEmpty(exam.PatologiaBinocular)) dxStack.Children.Add(CreateFieldRow("Patología", exam.PatologiaBinocular));
            if (!string.IsNullOrEmpty(exam.Observaciones)) dxStack.Children.Add(CreateFieldRow("Observaciones", exam.Observaciones));
            if (!string.IsNullOrEmpty(exam.SugerenciaDiseno)) dxStack.Children.Add(CreateFieldRow("Sugerencia", $"{exam.SugerenciaDiseno} {exam.SugerenciaMaterial}"));

            if (dxStack.Children.Count > 0)
            {
                contentStack.Children.Add(CreateSectionTitle("DIAGNÓSTICO Y PLAN"));
                contentStack.Children.Add(CreateSectionBox(dxStack));
            }

            mainStack.Children.Add(contentStack);
            cardBorder.Child = mainStack;
            return cardBorder;
        }

        private Border CreateOrderCard(Order order)
        {
            var card = new Border
            {
                BorderBrush = _borderColor,
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(6),
                Background = new SolidColorBrush(Microsoft.UI.Colors.White),
                Margin = new Thickness(0, 0, 0, 10),
                Padding = new Thickness(15)
            };
            var stack = new StackPanel { Spacing = 5 };

            // Título
            stack.Children.Add(new TextBlock { Text = $"👓 PEDIDO: {order.Date}", FontWeight = FontWeights.Bold, Foreground = _primaryColor });

            // Línea
            stack.Children.Add(new Rectangle { Height = 1, Fill = _borderColor, Margin = new Thickness(0, 5, 0, 5) });

            // Contenido
            var grid = new Grid();
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            var s1 = new StackPanel();
            s1.Children.Add(CreateFieldRow("Montura", order.FrameModel));
            s1.Children.Add(CreateFieldRow("Lentes", order.LensType));

            var s2 = new StackPanel { HorizontalAlignment = HorizontalAlignment.Right };
            s2.Children.Add(new TextBlock { Text = $"Total: {order.TotalAmount:C2}", FontWeight = FontWeights.Bold, HorizontalAlignment = HorizontalAlignment.Right });
            s2.Children.Add(new TextBlock { Text = string.IsNullOrEmpty(order.Status) ? "REGISTRADO" : order.Status.ToUpper(), Foreground = _primaryColor, HorizontalAlignment = HorizontalAlignment.Right, FontSize = 11 });

            grid.Children.Add(s1);
            Grid.SetColumn(s2, 1); grid.Children.Add(s2);

            stack.Children.Add(grid);
            card.Child = stack;
            return card;
        }

        // ==========================================
        //            COMPONENTES VISUALES
        // ==========================================

        private Border CreateSectionBox(UIElement content)
        {
            return new Border
            {
                Background = _sectionBgColor,
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Child = content
            };
        }

        private TextBlock CreateSectionTitle(string title)
        {
            return new TextBlock
            {
                Text = title,
                FontSize = 10,
                FontWeight = FontWeights.Bold,
                Foreground = _primaryColor,
                Margin = new Thickness(0, 0, 0, 4),
                CharacterSpacing = 50
            };
        }

        private TextBlock CreateSubTitle(string title)
        {
            return new TextBlock
            {
                Text = title,
                FontSize = 11,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = _labelColor,
                Margin = new Thickness(0, 0, 0, 2)
            };
        }

        private StackPanel CreateFieldRow(string label, string value)
        {
            var p = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 5, Margin = new Thickness(0, 2, 0, 2) };
            p.Children.Add(new TextBlock { Text = $"{label}:", FontWeight = FontWeights.SemiBold, Foreground = _labelColor, FontSize = 12 });
            p.Children.Add(new TextBlock { Text = value, Foreground = _valueColor, FontSize = 12, TextWrapping = TextWrapping.Wrap });
            return p;
        }

        // ==========================================
        //             TABLA DE REFRACCIÓN
        // ==========================================

        private Grid CreateRefractionTable(
            string sphOD, string cylOD, string axOD, string addOD,
            string sphOI, string cylOI, string axOI, string addOI,
            string avOD = null, string avOI = null)
        {
            var grid = new Grid { BorderBrush = _borderColor, BorderThickness = new Thickness(1), CornerRadius = new CornerRadius(4), Background = new SolidColorBrush(Microsoft.UI.Colors.White) };

            for (int i = 0; i < 6; i++) grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Header
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // OD
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // OI

            var headerBg = new Border { Background = _sectionBgColor, BorderBrush = _borderColor, BorderThickness = new Thickness(0, 0, 0, 1) };
            Grid.SetColumnSpan(headerBg, 6); grid.Children.Add(headerBg);

            AddCell(grid, "OJO", 0, 0, true);
            AddCell(grid, "ESF", 0, 1, true);
            AddCell(grid, "CIL", 0, 2, true);
            AddCell(grid, "EJE", 0, 3, true);
            AddCell(grid, "ADD", 0, 4, true);
            AddCell(grid, "AV", 0, 5, true);

            AddCell(grid, "OD", 1, 0, true, true);
            AddCell(grid, sphOD, 1, 1);
            AddCell(grid, cylOD, 1, 2);
            AddCell(grid, axOD, 1, 3);
            AddCell(grid, addOD, 1, 4);
            AddCell(grid, avOD, 1, 5);

            AddCell(grid, "OI", 2, 0, true, true);
            AddCell(grid, sphOI, 2, 1);
            AddCell(grid, cylOI, 2, 2);
            AddCell(grid, axOI, 2, 3);
            AddCell(grid, addOI, 2, 4);
            AddCell(grid, avOI, 2, 5);

            return grid;
        }

        private void AddCell(Grid g, string text, int row, int col, bool isHeader = false, bool isBlue = false)
        {
            var tb = new TextBlock
            {
                Text = string.IsNullOrEmpty(text) ? "-" : text,
                FontSize = 11,
                HorizontalAlignment = HorizontalAlignment.Center,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = isBlue ? _primaryColor : (isHeader ? _labelColor : _valueColor),
                FontWeight = (isHeader || isBlue) ? FontWeights.Bold : FontWeights.Normal,
                Margin = new Thickness(4)
            };
            Grid.SetRow(tb, row);
            Grid.SetColumn(tb, col);
            g.Children.Add(tb);
        }

        private TextBlock CreateEmptyMessage(string msg)
        {
            return new TextBlock
            {
                Text = msg,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Foreground = _labelColor,
                Margin = new Thickness(0, 10, 0, 10),
                HorizontalAlignment = HorizontalAlignment.Center
            };
        }

        private bool AreAllEmpty(params string[] values) => values.All(string.IsNullOrEmpty);

        // ==========================================
        //  MÉTODO DE IMPRESIÓN (PDF) - CORREGIDO
        // ==========================================
        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            // Evitar doble clic
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                if (_patient == null) return;

                // 1. Configurar dónde guardar el PDF
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Documento PDF", new List<string>() { ".pdf" });
                savePicker.SuggestedFileName = $"Historial_{_patient.FullName.Replace(" ", "_")}";

                // Truco para WinUI 3 (Asociar ventana)
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

                // 2. Mostrar diálogo
                var file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    // 3. Obtener datos necesarios para el reporte
                    var exams = PatientRepository.GetExamsByPatientId(_patient.Id);
                    var orders = PatientRepository.GetOrdersByPatientId(_patient.Id);

                    // 4. Generar el PDF usando el nuevo PrintService
                    var printService = new PrintService(); // Constructor vacío (Correcto)
                    printService.GenerateHistory(_patient, exams, orders, file.Path); // Método nuevo (Correcto)

                    // 5. Abrir el archivo generado
                    await Windows.System.Launcher.LaunchFileAsync(file);
                }
            }
            catch (Exception ex)
            {
                // Mostrar error si falla
                var dialog = new ContentDialog
                {
                    Title = "Error al exportar",
                    Content = $"No se pudo crear el PDF: {ex.Message}",
                    CloseButtonText = "Ok",
                    XamlRoot = this.Content.XamlRoot
                };
                await dialog.ShowAsync();
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}