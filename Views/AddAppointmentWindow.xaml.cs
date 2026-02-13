using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using WinRT;

namespace OpticaPro.Views
{
    public sealed partial class AddAppointmentWindow : Window
    {
        private Appointment _appointmentToEdit;
        private bool _isEditing = false;

        // Variable para guardar el paciente seleccionado tras la búsqueda
        private Patient _selectedPatient;

        public AddAppointmentWindow()
        {
            this.InitializeComponent();
            InitializeWindow();
            // Eliminamos LoadPatients() para optimizar
        }

        public AddAppointmentWindow(Patient patient) : this()
        {
            if (patient != null)
            {
                SetSelectedPatient(patient);
            }
        }

        public AddAppointmentWindow(Appointment appointment) : this()
        {
            if (appointment != null)
            {
                _appointmentToEdit = appointment;
                _isEditing = true;
                LoadAppointmentData();
            }
        }

        private void InitializeWindow()
        {
            try
            {
                IntPtr hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WindowId windowId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
                AppWindow appWindow = AppWindow.GetFromWindowId(windowId);

                if (appWindow != null)
                {
                    appWindow.Resize(new Windows.Graphics.SizeInt32(600, 750));
                    // \u00F3 = ó
                    appWindow.Title = "Gesti\u00F3n de Citas - OpticaPro";
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error configurando ventana: {ex.Message}");
            }
        }

        // Método centralizado para asignar un paciente y actualizar la UI
        private void SetSelectedPatient(Patient patient)
        {
            _selectedPatient = patient;
            if (_selectedPatient != null)
            {
                TxtPatientName.Text = _selectedPatient.FullName;
                PatientDetailPanel.Visibility = Visibility.Visible;
                // \u00E9 = é, \u00F3 = ó
                LblPatientPhone.Text = $"📞 Tel\u00E9fono: {_selectedPatient.Phone ?? "No registrado"}";
                LblPatientCity.Text = $"📍 Ubicaci\u00F3n: {_selectedPatient.City ?? "No registrada"}";
            }
        }

        private void LoadAppointmentData()
        {
            if (_appointmentToEdit == null) return;

            this.Title = "Editar Cita Existente";

            // Intentamos buscar el paciente en la BD actual
            var allPatients = PatientRepository.GetAllPatients();
            var patient = allPatients.FirstOrDefault(p => p.Id == _appointmentToEdit.PatientId);

            if (patient != null)
            {
                SetSelectedPatient(patient);
            }
            else
            {
                // Si el paciente fue borrado o no se encuentra, mostramos el nombre guardado en la cita
                TxtPatientName.Text = _appointmentToEdit.PatientName;
            }

            if (DateTime.TryParse(_appointmentToEdit.Date, out DateTime dateValue))
            {
                PickDate.Date = new DateTimeOffset(dateValue);
            }

            if (TimeSpan.TryParse(_appointmentToEdit.Time, out TimeSpan timeValue))
            {
                PickTime.SelectedTime = timeValue;
            }

            TxtReason.Text = _appointmentToEdit.Reason;
        }

        // --- LÓGICA DE BÚSQUEDA (VENTANA EMERGENTE) ---
        private async void BtnSearchPatient_Click(object sender, RoutedEventArgs e)
        {
            // 1. Construimos la UI del diálogo dinámicamente
            var container = new StackPanel { Spacing = 12, MinWidth = 350 };

            // TextBox corregido (sin .Icon)
            var txtSearch = new TextBox
            {
                PlaceholderText = "Escriba nombre o cédula..."
            };

            var listView = new ListView
            {
                Height = 250,
                BorderThickness = new Thickness(1),
                BorderBrush = (Microsoft.UI.Xaml.Media.Brush)Application.Current.Resources["CardStrokeColorDefaultBrush"],
                CornerRadius = new CornerRadius(4)
            };

            // Template para mostrar Nombre y Cédula en cada fila
            string xamlTemplate = @"
                <DataTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation'>
                    <StackPanel Padding='10,8'>
                        <TextBlock Text='{Binding FullName}' FontWeight='SemiBold'/>
                        <TextBlock Text='{Binding Dni}' FontSize='12' Foreground='Gray' Margin='0,2,0,0'/>
                    </StackPanel>
                </DataTemplate>";

            listView.ItemTemplate = (DataTemplate)Microsoft.UI.Xaml.Markup.XamlReader.Load(xamlTemplate);

            container.Children.Add(txtSearch);
            container.Children.Add(listView);

            // 2. Cargamos datos y configuramos filtrado
            var allPatients = PatientRepository.GetAllPatients();
            listView.ItemsSource = allPatients; // Mostrar todos al inicio

            txtSearch.TextChanged += (s, args) =>
            {
                var query = txtSearch.Text.ToLower();
                if (string.IsNullOrWhiteSpace(query))
                {
                    listView.ItemsSource = allPatients;
                }
                else
                {
                    listView.ItemsSource = allPatients.Where(p =>
                        (p.FullName != null && p.FullName.ToLower().Contains(query)) ||
                        (p.Dni != null && p.Dni.Contains(query))
                    ).ToList();
                }
            };

            // 3. Crear el diálogo
            ContentDialog dialog = new ContentDialog
            {
                XamlRoot = this.Content.XamlRoot,
                Title = "Buscar Paciente",
                Content = container,
                PrimaryButtonText = "Cancelar",
                DefaultButton = ContentDialogButton.Primary
            };

            // 4. Manejar la selección
            listView.SelectionChanged += (s, args) =>
            {
                if (listView.SelectedItem is Patient selected)
                {
                    SetSelectedPatient(selected);
                    dialog.Hide(); // Cerramos el diálogo al seleccionar
                }
            };

            await dialog.ShowAsync();
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            InfoStatus.IsOpen = false;

            if (_selectedPatient == null && !_isEditing)
            {
                ShowError("Debe seleccionar un paciente para continuar.");
                return;
            }

            // Si estamos editando y no cambiamos el paciente, usamos los datos previos
            string patientIdToSave = _selectedPatient?.Id ?? _appointmentToEdit?.PatientId;
            string patientNameToSave = _selectedPatient?.FullName ?? _appointmentToEdit?.PatientName;

            if (string.IsNullOrEmpty(patientIdToSave))
            {
                ShowError("Error identificando al paciente.");
                return;
            }

            if (!PickTime.SelectedTime.HasValue)
            {
                // \u00E1 = á
                ShowError("Por favor, seleccione una hora v\u00E1lida.");
                return;
            }

            if (string.IsNullOrWhiteSpace(TxtReason.Text))
            {
                ShowError("Es necesario ingresar un motivo para la consulta.");
                return;
            }

            try
            {
                string dateStr = PickDate.Date.ToString("dd/MM/yyyy");
                string timeStr = PickTime.SelectedTime.Value.ToString(@"hh\:mm");
                string reason = TxtReason.Text.Trim();

                if (_isEditing && _appointmentToEdit != null)
                {
                    _appointmentToEdit.PatientId = patientIdToSave;
                    _appointmentToEdit.PatientName = patientNameToSave;
                    _appointmentToEdit.Date = dateStr;
                    _appointmentToEdit.Time = timeStr;
                    _appointmentToEdit.Reason = reason;

                    AppointmentRepository.UpdateAppointment(_appointmentToEdit);
                }
                else
                {
                    var newAppt = new Appointment
                    {
                        Id = Guid.NewGuid().ToString(),
                        PatientId = patientIdToSave,
                        PatientName = patientNameToSave,
                        Date = dateStr,
                        Time = timeStr,
                        Reason = reason,
                        Status = "Pendiente"
                    };

                    AppointmentRepository.AddAppointment(newAppt);
                }

                this.Close();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error crítico al guardar: {ex.Message}");
                // \u00F3 = ó
                ShowError("Ocurri\u00F3 un error interno al intentar guardar.");
            }
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ShowError(string message)
        {
            InfoStatus.Severity = InfoBarSeverity.Error;
            // \u00F3 = ó
            InfoStatus.Title = "Atenci\u00F3n";
            InfoStatus.Message = message;
            InfoStatus.IsOpen = true;
        }
    }
}