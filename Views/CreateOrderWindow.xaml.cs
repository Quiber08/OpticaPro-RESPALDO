using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Markup;
using OpticaPro.Models;
using OpticaPro.Services;
using Windows.UI;

namespace OpticaPro.Views
{
    public sealed partial class CreateOrderWindow : Window
    {
        private Patient _currentPatient;
        private OrderDisplayItem _orderToEdit;
        private List<Product> _allProducts;
        private ClinicalExam _selectedExam;

        // Constructor para NUEVO pedido
        public CreateOrderWindow(Patient patient)
        {
            this.InitializeComponent();
            _currentPatient = patient;
            SetupWindow("Nueva Orden de Trabajo");

            // Fecha de entrega sugerida: 3 días después de hoy
            DateTxt.Text = DateTime.Now.AddDays(3).ToString("dd/MM/yyyy");
            LoadPatientExams();
        }

        // Constructor para EDITAR pedido existente
        public CreateOrderWindow(OrderDisplayItem item)
        {
            this.InitializeComponent();
            _orderToEdit = item;
            _currentPatient = item.PatientOwner;
            SetupWindow("Editar Orden de Trabajo");

            LoadPatientExams();
            LoadOrderData();
        }

        private void SetupWindow(string title)
        {
            this.Title = title;
            if (_currentPatient != null)
                PatientNameTitle.Text = $"Cliente: {_currentPatient.FullName}";

            try { this.AppWindow.Resize(new Windows.Graphics.SizeInt32(950, 780)); } catch { }
        }

        // =========================================================
        //  1. SELECCIÓN DE EXAMEN (VENTANA EMERGENTE + PREVISUALIZACIÓN)
        // =========================================================

        private async void SearchExamBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPatient.ExamHistory == null || !_currentPatient.ExamHistory.Any())
            {
                // INTENTO DE RECUPERACIÓN: Si la lista en memoria está vacía, buscamos en BD
                var examenesBD = PatientRepository.GetExamsByPatientId(_currentPatient.Id);
                if (examenesBD != null && examenesBD.Count > 0)
                {
                    _currentPatient.ExamHistory = examenesBD;
                }
                else
                {
                    var noDataDialog = new ContentDialog
                    {
                        Title = "Sin Historial",
                        Content = "Este paciente no tiene exámenes registrados.",
                        CloseButtonText = "Aceptar",
                        XamlRoot = this.Content.XamlRoot
                    };
                    await noDataDialog.ShowAsync();
                    return;
                }
            }

            ContentDialog dialog = new ContentDialog
            {
                Title = "Seleccionar Examen",
                PrimaryButtonText = "Confirmar Selección",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            StackPanel panel = new StackPanel { Spacing = 15, MinWidth = 400 };

            TextBox searchBox = new TextBox
            {
                PlaceholderText = "Filtrar por fecha...",
                Header = "Buscar en Historial",
                InputScope = new Microsoft.UI.Xaml.Input.InputScope { Names = { new Microsoft.UI.Xaml.Input.InputScopeName(Microsoft.UI.Xaml.Input.InputScopeNameValue.Search) } }
            };

            ListView examList = new ListView
            {
                Height = 250,
                SelectionMode = ListViewSelectionMode.Single,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                CornerRadius = new CornerRadius(4),
                ItemsSource = _currentPatient.ExamHistory.OrderByDescending(x => x.Date).ToList()
            };

            string examTemplate = @"
                <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                    <Grid Padding='10'>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width='Auto'/>
                            <ColumnDefinition Width='*'/>
                        </Grid.ColumnDefinitions>
                        <Border Grid.Column='0' Background='#F0F3F9' Width='40' Height='40' CornerRadius='20' Margin='0,0,15,0'>
                            <FontIcon Glyph='&#xE787;' FontFamily='Segoe MDL2 Assets' Foreground='#0078D7' FontSize='16' HorizontalAlignment='Center' VerticalAlignment='Center'/>
                        </Border>
                        <StackPanel Grid.Column='1' VerticalAlignment='Center'>
                            <TextBlock Text='{Binding Date}' FontWeight='SemiBold' FontSize='15'/>
                            <TextBlock Text='Examen Clínico' Foreground='Gray' FontSize='12'/>
                        </StackPanel>
                    </Grid>
                </DataTemplate>";

            try { examList.ItemTemplate = (DataTemplate)XamlReader.Load(examTemplate); } catch { }

            searchBox.TextChanged += (s, args) =>
            {
                var query = searchBox.Text.ToLower().Trim();
                if (string.IsNullOrEmpty(query)) examList.ItemsSource = _currentPatient.ExamHistory.OrderByDescending(x => x.Date).ToList();
                else examList.ItemsSource = _currentPatient.ExamHistory
                        .Where(x => x.Date != null && x.Date.Contains(query))
                        .OrderByDescending(x => x.Date).ToList();
            };

            panel.Children.Add(searchBox);
            panel.Children.Add(examList);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && examList.SelectedItem is ClinicalExam selected)
            {
                SetSelectedExam(selected);
            }
        }

        private void SetSelectedExam(ClinicalExam exam)
        {
            _selectedExam = exam;

            if (_selectedExam != null)
            {
                ExamTxt.Text = $"Examen del {_selectedExam.Date}";

                ExamPreviewGrid.Visibility = Visibility.Visible;
                NoExamMessage.Visibility = Visibility.Collapsed;

                TxtSphOD.Text = string.IsNullOrEmpty(exam.SphereOD) ? "N/A" : exam.SphereOD;
                TxtCylOD.Text = string.IsNullOrEmpty(exam.CylOD) ? "-" : exam.CylOD;
                TxtAxisOD.Text = string.IsNullOrEmpty(exam.AxisOD) ? "-" : exam.AxisOD;
                TxtAddOD.Text = string.IsNullOrEmpty(exam.AddOD) ? "" : "ADD: " + exam.AddOD;

                TxtSphOI.Text = string.IsNullOrEmpty(exam.SphereOI) ? "N/A" : exam.SphereOI;
                TxtCylOI.Text = string.IsNullOrEmpty(exam.CylOI) ? "-" : exam.CylOI;
                TxtAxisOI.Text = string.IsNullOrEmpty(exam.AxisOI) ? "-" : exam.AxisOI;
                TxtAddOI.Text = string.IsNullOrEmpty(exam.AddOI) ? "" : "ADD: " + exam.AddOI;
            }
            else
            {
                ExamTxt.Text = "";
                ExamPreviewGrid.Visibility = Visibility.Collapsed;
                NoExamMessage.Visibility = Visibility.Visible;
            }
        }

        private void LoadPatientExams()
        {
            if (_currentPatient.ExamHistory != null && _currentPatient.ExamHistory.Any())
            {
                var latestExam = _currentPatient.ExamHistory.OrderByDescending(x => x.Date).FirstOrDefault();
                SetSelectedExam(latestExam);
                SearchExamBtn.IsEnabled = true;
            }
            else
            {
                var examenesBD = PatientRepository.GetExamsByPatientId(_currentPatient.Id);
                if (examenesBD != null && examenesBD.Any())
                {
                    _currentPatient.ExamHistory = examenesBD;
                    SetSelectedExam(examenesBD.First());
                    SearchExamBtn.IsEnabled = true;
                }
                else
                {
                    ExamTxt.Text = "No hay exámenes registrados";
                    ExamPreviewGrid.Visibility = Visibility.Collapsed;
                    NoExamMessage.Text = "Este paciente no tiene historial clínico.";
                    SearchExamBtn.IsEnabled = false;
                }
            }
        }

        // =========================================================
        //  2. BUSCADOR DE INVENTARIO (ARMAZONES)
        // =========================================================

        private async void SearchFrameBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_allProducts == null) _allProducts = InventoryRepository.GetAllProducts();

            ContentDialog dialog = new ContentDialog
            {
                Title = "Buscador de Inventario",
                PrimaryButtonText = "Seleccionar",
                CloseButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary,
                XamlRoot = this.Content.XamlRoot
            };

            StackPanel panel = new StackPanel { Spacing = 15, MinWidth = 450 };

            TextBox searchBox = new TextBox
            {
                PlaceholderText = "Escribe código, marca o nombre...",
                Header = "Buscar Armazón",
                InputScope = new Microsoft.UI.Xaml.Input.InputScope { Names = { new Microsoft.UI.Xaml.Input.InputScopeName(Microsoft.UI.Xaml.Input.InputScopeNameValue.Search) } }
            };

            ListView resultsList = new ListView
            {
                Height = 300,
                SelectionMode = ListViewSelectionMode.Single,
                BorderThickness = new Thickness(1),
                BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.LightGray),
                CornerRadius = new CornerRadius(4),
                ItemsSource = _allProducts
            };

            // Intentamos usar el recurso si existe, sino lo creamos en código
            if (this.Content is FrameworkElement root && root.Resources.TryGetValue("ProductSearchTemplate", out object template))
            {
                resultsList.ItemTemplate = (DataTemplate)template;
            }
            else
            {
                string prodTemplate = @"
                    <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                        <StackPanel Padding='10'>
                            <TextBlock Text='{Binding Brand}' FontWeight='Bold'/>
                            <TextBlock Text='{Binding Name}'/>
                            <TextBlock Text='{Binding Code}' Foreground='Gray' FontSize='12'/>
                        </StackPanel>
                    </DataTemplate>";
                try { resultsList.ItemTemplate = (DataTemplate)XamlReader.Load(prodTemplate); } catch { }
            }

            searchBox.TextChanged += (s, args) =>
            {
                var query = searchBox.Text.ToLower().Trim();
                if (string.IsNullOrEmpty(query)) resultsList.ItemsSource = _allProducts;
                else resultsList.ItemsSource = _allProducts.Where(p =>
                        (p.Name != null && p.Name.ToLower().Contains(query)) ||
                        (p.Brand != null && p.Brand.ToLower().Contains(query)) ||
                        (p.Code != null && p.Code.ToLower().Contains(query))
                    ).ToList();
            };

            panel.Children.Add(searchBox);
            panel.Children.Add(resultsList);
            dialog.Content = panel;

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary && resultsList.SelectedItem is Product selectedProduct)
            {
                FrameTxt.Text = $"{selectedProduct.Brand} {selectedProduct.Name} (Cód: {selectedProduct.Code})";
            }
        }

        // =========================================================
        //  3. RESTO DE LÓGICA (GUARDAR, CÁLCULOS)
        // =========================================================

        private void LoadOrderData()
        {
            if (_orderToEdit == null) return;

            var data = _orderToEdit.OrderData;

            FrameTxt.Text = data.FrameModel;
            LensTxt.Text = data.LensType;
            DateTxt.Text = data.DeliveryDate;
            LabTxt.Text = data.Laboratory;
            LabCostTxt.Text = data.LabCost.ToString();
            TotalTxt.Text = data.TotalAmount.ToString();
            DepositTxt.Text = data.Deposit.ToString();

            if (data.Status == "Plan Acumulativo")
            {
                AccumulativePlanSwitch.IsOn = true;
            }

            CalculateBalance();
        }

        // Este es el método que faltaba y causaba el error CS1061
        private void AccumulativePlanSwitch_Toggled(object sender, RoutedEventArgs e)
        {
            if (AccumulativePlanSwitch.IsOn) DepositTxt.Header = "Primer Pago / Abono";
            else DepositTxt.Header = "Abono Inicial ($)";
        }

        private void CalculateBalance_TextChanged(object sender, TextChangedEventArgs e)
        {
            CalculateBalance();
        }

        private void CalculateBalance()
        {
            decimal.TryParse(TotalTxt.Text, out decimal total);
            decimal.TryParse(DepositTxt.Text, out decimal deposit);

            decimal balance = total - deposit;
            BalanceLabel.Text = balance.ToString("C2");

            if (balance < 0) BalanceLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Red);
            else if (balance == 0) BalanceLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.Green);
            else BalanceLabel.Foreground = new SolidColorBrush(Microsoft.UI.Colors.OrangeRed);
        }

        // =========================================================
        //  GUARDADO CORREGIDO (SOLUCIÓN DEFINITIVA)
        // =========================================================
        private void SaveOrder_Click(object sender, RoutedEventArgs e)
        {
            // 1. Validaciones básicas
            if (string.IsNullOrWhiteSpace(FrameTxt.Text) || string.IsNullOrWhiteSpace(LensTxt.Text))
            {
                ShowError("Debes ingresar la Montura y el Tipo de Lente.");
                return;
            }

            decimal.TryParse(TotalTxt.Text, out decimal total);
            decimal.TryParse(DepositTxt.Text, out decimal abono);
            decimal.TryParse(LabCostTxt.Text, out decimal costo);

            // 2. Determinar estado
            string estado = "En Taller";
            if (AccumulativePlanSwitch.IsOn) estado = "Plan Acumulativo";
            else if (total > 0 && total <= abono) estado = "Pagado / En Taller";

            // 3. Preparar info del examen seleccionado
            string examInfo = "";
            if (_selectedExam != null)
            {
                examInfo = $" [Medida: {_selectedExam.Date}]";
            }

            // 4. Crear o Actualizar Objeto ORDER
            Order orderToSave;

            if (_orderToEdit != null)
            {
                // Modo EDICIÓN: Usamos el existente
                orderToSave = _orderToEdit.OrderData;
            }
            else
            {
                // Modo NUEVO: Creamos uno nuevo
                orderToSave = new Order();
                orderToSave.Date = DateTime.Now.ToString("dd/MM/yyyy");
                orderToSave.Status = "Pendiente";
                orderToSave.ClientName = _currentPatient.FullName;

                // --- CORRECCIÓN AQUÍ: Convertimos el ID (int) a String ---
                orderToSave.PatientId = _currentPatient.Id.ToString();
            }

            // 5. Asignar valores del formulario
            orderToSave.FrameModel = FrameTxt.Text;

            // Evitar duplicar la info del examen si ya se editó antes
            if (_orderToEdit != null && (orderToSave.LensType?.Contains("[Medida:") == true))
                orderToSave.LensType = LensTxt.Text;
            else
                orderToSave.LensType = LensTxt.Text + examInfo;

            orderToSave.DeliveryDate = DateTxt.Text;
            orderToSave.Laboratory = LabTxt.Text;
            orderToSave.LabCost = costo;
            orderToSave.TotalAmount = total;
            orderToSave.Deposit = abono;

            // Recálculo del saldo
            orderToSave.Balance = total - abono;
            orderToSave.Status = estado;

            try
            {
                // 6. GUARDAR EN LA BASE DE DATOS
                PatientRepository.SaveOrder(orderToSave);
                this.Close();
            }
            catch (Exception ex)
            {
                ShowError($"Error al guardar en base de datos: {ex.Message}");
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private async void ShowError(string msg)
        {
            var dialog = new ContentDialog
            {
                Title = "Atención",
                Content = msg,
                CloseButtonText = "Entendido",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}