using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Linq; // Necesario para validar números
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace OpticaPro.Views
{
    public sealed partial class AddPatientPage : Page
    {
        private Patient _existingPatient;
        private bool _isEditingMode = false;

        public AddPatientPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Patient patientToEdit)
            {
                // MODO EDICIÓN
                _existingPatient = patientToEdit;
                _isEditingMode = true;

                LoadPatientData();

                PageTitle.Text = "Editar Paciente";
                // \u00F3 = ó
                PageSubtitle.Text = $"Modificando expediente de {_existingPatient.FullName}";
                BtnSave.Content = "Guardar Cambios";
            }
            else
            {
                // MODO CREACIÓN
                _isEditingMode = false;
                _existingPatient = null;

                PageTitle.Text = "Registrar Nuevo Paciente";
                PageSubtitle.Text = "Complete la informaci\u00F3n cl\u00EDnica y personal requerida.";
                BtnSave.Content = "Guardar Paciente";
            }
        }

        private void LoadPatientData()
        {
            if (_existingPatient == null) return;

            TxtFullName.Text = _existingPatient.FullName ?? "";
            TxtDni.Text = _existingPatient.Dni ?? "";
            TxtPhone.Text = _existingPatient.Phone ?? "";
            TxtEmail.Text = _existingPatient.Email ?? "";
            TxtAddress.Text = _existingPatient.Address ?? "";
            TxtOccupation.Text = _existingPatient.Occupation ?? "";
            TxtCity.Text = _existingPatient.City ?? "";

            // CORRECCIÓN LINEA 65: Asignación directa porque Age ya es entero
            NumAge.Value = _existingPatient.Age;
        }

        // --- LÓGICA DE RESTRICCIÓN (SOLO NÚMEROS) ---
        // Este evento evita que se escriban letras en DNI y Teléfono
        private void TxtNumeric_BeforeTextChanging(TextBox sender, TextBoxBeforeTextChangingEventArgs args)
        {
            // Solo permitimos dígitos (0-9)
            if (!string.IsNullOrEmpty(args.NewText) && !args.NewText.All(char.IsDigit))
            {
                args.Cancel = true; // Cancelar si hay letras
            }
        }

        private void BtnSave_Click(object sender, RoutedEventArgs e)
        {
            FormInfoBar.IsOpen = false;

            if (!ValidateForm())
            {
                return;
            }

            try
            {
                SavePatientToDatabase();
            }
            catch (Exception ex)
            {
                ShowError($"Error cr\u00EDtico al guardar: {ex.Message}");
            }
        }

        private bool ValidateForm()
        {
            if (string.IsNullOrWhiteSpace(TxtFullName.Text))
            {
                ShowError("El nombre del paciente es obligatorio.");
                TxtFullName.Focus(FocusState.Programmatic);
                return false;
            }

            if (TxtFullName.Text.Length < 3)
            {
                ShowError("El nombre es demasiado corto. Ingrese nombre y apellido.");
                TxtFullName.Focus(FocusState.Programmatic);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtDni.Text))
            {
                ShowError("La C\u00E9dula o DNI es obligatoria.");
                TxtDni.Focus(FocusState.Programmatic);
                return false;
            }

            if (TxtDni.Text.Length != 10)
            {
                ShowError("La C\u00E9dula debe tener exactamente 10 d\u00EDgitos.");
                TxtDni.Focus(FocusState.Programmatic);
                return false;
            }

            if (string.IsNullOrWhiteSpace(TxtPhone.Text))
            {
                ShowError("El tel\u00E9fono es obligatorio.");
                TxtPhone.Focus(FocusState.Programmatic);
                return false;
            }

            if (TxtPhone.Text.Length != 10)
            {
                ShowError("El tel\u00E9fono debe tener 10 d\u00EDgitos.");
                TxtPhone.Focus(FocusState.Programmatic);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(TxtEmail.Text))
            {
                string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
                if (!Regex.IsMatch(TxtEmail.Text, emailPattern))
                {
                    ShowError("El formato del correo electr\u00F3nico no es v\u00E1lido.");
                    TxtEmail.Focus(FocusState.Programmatic);
                    return false;
                }
            }

            return true;
        }

        private void SavePatientToDatabase()
        {
            // Obtener valores limpios
            string fullName = TxtFullName.Text.Trim();
            string dni = TxtDni.Text.Trim();
            string phone = TxtPhone.Text.Trim();
            string email = TxtEmail.Text.Trim();
            string address = TxtAddress.Text.Trim();
            string occupation = TxtOccupation.Text.Trim();
            string city = TxtCity.Text.Trim();

            // Obtenemos la edad como entero
            int ageInt = (int)NumAge.Value;

            if (_isEditingMode && _existingPatient != null)
            {
                // --- ACTUALIZAR EXISTENTE ---
                _existingPatient.FullName = fullName;
                _existingPatient.Dni = dni;
                _existingPatient.Phone = phone;
                _existingPatient.Email = email;
                _existingPatient.Address = address;
                _existingPatient.Occupation = occupation;
                _existingPatient.City = city;

                // CORRECCIÓN LINEA 193: Asignación directa int -> int
                _existingPatient.Age = ageInt;

                PatientRepository.UpdatePatient(_existingPatient);

                ShowSuccess("Paciente actualizado correctamente.");
            }
            else
            {
                // --- CREAR NUEVO ---
                var newPatient = new Patient
                {
                    Id = Guid.NewGuid().ToString(),
                    FullName = fullName,
                    Dni = dni,
                    Phone = phone,
                    Email = email,
                    Address = address,
                    Occupation = occupation,
                    City = city,

                    // CORRECCIÓN LINEA 212: Asignación directa int -> int
                    Age = ageInt
                };

                PatientRepository.AddPatient(newPatient);

                ShowSuccess("Paciente registrado exitosamente.");
            }
        }

        private void ShowError(string message)
        {
            FormInfoBar.Title = "Atenci\u00F3n";
            FormInfoBar.Message = message;
            FormInfoBar.Severity = InfoBarSeverity.Error;
            FormInfoBar.IsOpen = true;
        }

        private async void ShowSuccess(string message)
        {
            FormInfoBar.Title = "\u00C9xito"; // Éxito
            FormInfoBar.Message = message;
            FormInfoBar.Severity = InfoBarSeverity.Success;
            FormInfoBar.IsOpen = true;

            await Task.Delay(1500);
            GoBack();
        }

        private void BtnCancel_Click(object sender, RoutedEventArgs e)
        {
            GoBack();
        }

        private void GoBack()
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
        }
    }
}