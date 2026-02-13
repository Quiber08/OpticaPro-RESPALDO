using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace OpticaPro.Views
{
    // Clase auxiliar para mostrar usuarios en la lista
    public class LoginUserDisplay
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsAdmin { get; set; }
    }

    public sealed partial class LoginWindow : Window
    {
        private LoginUserDisplay _selectedUser;

        public LoginWindow()
        {
            this.InitializeComponent();

            // Protección por si falla el cambio de tamaño
            try { this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1050, 650)); } catch { }

            CheckStatus();
        }

        private void CheckStatus()
        {
            if (SecurityService.IsAppInitialized())
            {
                PanelLicense.Visibility = Visibility.Collapsed;
                PanelRegister.Visibility = Visibility.Collapsed;
                LoadUsersList();
            }
            else
            {
                PanelLicense.Visibility = Visibility.Visible;
                PanelRegister.Visibility = Visibility.Collapsed;
                UserSelectionList.Visibility = Visibility.Collapsed;
                PanelPassword.Visibility = Visibility.Collapsed;

                LblWelcome.Text = "Activación Requerida";
                string machineId = SecurityService.GetMachineId();
                LblInstruction.Text = $"ID de Instalación: {machineId}\nEnvía este código al proveedor para obtener tu clave.";

                try
                {
                    var package = new Windows.ApplicationModel.DataTransfer.DataPackage();
                    package.SetText(machineId);
                    Windows.ApplicationModel.DataTransfer.Clipboard.SetContent(package);
                }
                catch { }
            }
        }

        private void LoadUsersList()
        {
            var displayList = new List<LoginUserDisplay>();

            // 1. Agregamos al Administrador/Dueño
            displayList.Add(new LoginUserDisplay
            {
                Username = SecurityService.GetAdminName(),
                FullName = "Administrador (Dueño)",
                Role = "Gerente General",
                IsAdmin = true
            });

            // 2. Agregamos a los empleados
            var employees = UserRepository.GetAllUsers();
            if (employees != null)
            {
                foreach (var emp in employees)
                {
                    displayList.Add(new LoginUserDisplay
                    {
                        Username = emp.Username,
                        FullName = emp.FullName,
                        Role = emp.Role,
                        IsAdmin = false
                    });
                }
            }

            UserSelectionList.ItemsSource = displayList;
            UserSelectionList.Visibility = Visibility.Visible;
            PanelPassword.Visibility = Visibility.Collapsed;
            LblWelcome.Text = "Bienvenido";
            LblInstruction.Text = "Selecciona tu perfil para ingresar.";
            LblError.Text = "";
        }

        private void UserItem_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            var user = btn?.Tag as LoginUserDisplay;

            if (user != null)
            {
                _selectedUser = user;
                UserSelectionList.Visibility = Visibility.Collapsed;
                PanelPassword.Visibility = Visibility.Visible;

                SelectedUserName.Text = user.FullName;
                SelectedUserPic.DisplayName = user.FullName;
                var names = user.FullName.Split(' ');
                LblWelcome.Text = $"Hola, {names[0]}";
                LblInstruction.Text = "Ingresa tu contraseña para confirmar.";

                TxtLoginPass.Password = "";
                TxtLoginPass.Focus(FocusState.Programmatic);
            }
        }

        private void BackToUsers_Click(object sender, RoutedEventArgs e)
        {
            _selectedUser = null;
            TxtLoginPass.Password = "";
            LoadUsersList();
        }

        // --- AQUÍ ESTÁ EL PUNTO CRÍTICO QUE SOLUCIONA EL CIERRE ---
        private void Login_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            try
            {
                bool success;
                if (_selectedUser.IsAdmin)
                    success = SecurityService.Login(SecurityService.GetAdminName(), TxtLoginPass.Password);
                else
                    success = SecurityService.Login(_selectedUser.Username, TxtLoginPass.Password);

                if (success)
                {
                    OpenMainWindow();
                }
                else
                {
                    LblError.Text = "Contraseña incorrecta.";
                    TxtLoginPass.Password = "";
                    TxtLoginPass.Focus(FocusState.Programmatic);
                }
            }
            catch (Exception ex)
            {
                // Si hay un error técnico (ej. base de datos), lo mostramos en lugar de cerrarnos
                LblError.Text = "Error de sistema: " + ex.Message;
            }
        }

        private void TxtLoginPass_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) Login_Click(sender, null);
        }

        private async void ForgotPassword_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedUser == null) return;

            if (!_selectedUser.IsAdmin)
            {
                await new ContentDialog
                {
                    Title = "Recuperación de Acceso",
                    Content = $"Solo el administrador puede restablecer las claves del personal.\n\nPor favor, contacta a {SecurityService.GetAdminName()} para que actualice tu contraseña.",
                    CloseButtonText = "Entendido",
                    XamlRoot = this.Content.XamlRoot
                }.ShowAsync();
            }
            else
            {
                TextBox txtLicenseVerify = new TextBox { Header = "Licencia del Producto", PlaceholderText = "Ingresa tu clave de activación" };
                PasswordBox txtNewPass = new PasswordBox { Header = "Nueva Contraseña", PlaceholderText = "Escribe tu nueva clave maestra" };

                StackPanel panel = new StackPanel { Spacing = 15 };
                panel.Children.Add(new TextBlock { Text = "Por seguridad, verifica tu identidad ingresando la licencia original.", Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray), TextWrapping = TextWrapping.Wrap });
                panel.Children.Add(txtLicenseVerify);
                panel.Children.Add(txtNewPass);

                ContentDialog dialog = new ContentDialog
                {
                    Title = "Restablecer Admin",
                    Content = panel,
                    PrimaryButtonText = "Cambiar Clave",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.Content.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    if (SecurityService.VerifyMasterLicense(txtLicenseVerify.Text))
                    {
                        if (!string.IsNullOrWhiteSpace(txtNewPass.Password))
                        {
                            SecurityService.UpdateAdminPassword(txtNewPass.Password);
                            await new ContentDialog { Title = "¡Éxito!", Content = "Contraseña actualizada. Úsala para ingresar.", CloseButtonText = "Ok", XamlRoot = this.Content.XamlRoot }.ShowAsync();
                        }
                        else
                        {
                            await new ContentDialog { Title = "Error", Content = "La contraseña no puede estar vacía.", CloseButtonText = "Ok", XamlRoot = this.Content.XamlRoot }.ShowAsync();
                        }
                    }
                    else
                    {
                        await new ContentDialog { Title = "Error", Content = "Licencia incorrecta. No se pudo verificar la identidad.", CloseButtonText = "Cerrar", XamlRoot = this.Content.XamlRoot }.ShowAsync();
                    }
                }
            }
        }

        private async void ValidateLicense_Click(object sender, RoutedEventArgs e)
        {
            TxtLicense.IsEnabled = false;
            LblError.Text = "Validando con el servidor...";

            try
            {
                var result = await SupabaseService.ValidateAndRegisterLicense(TxtLicense.Text);

                if (result.isValid)
                {
                    LblError.Text = "";
                    PanelLicense.Visibility = Visibility.Collapsed;
                    PanelRegister.Visibility = Visibility.Visible;

                    if (!string.IsNullOrEmpty(result.clientName))
                    {
                        TxtBusinessName.Text = result.clientName;
                        LblWelcome.Text = $"Hola, {result.clientName}";
                    }
                }
                else
                {
                    LblError.Text = result.message;
                }
            }
            catch (Exception ex)
            {
                LblError.Text = "Error de conexión: " + ex.Message;
            }

            TxtLicense.IsEnabled = true;
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtBusinessName.Text) ||
                string.IsNullOrWhiteSpace(TxtRegUser.Text) ||
                string.IsNullOrWhiteSpace(TxtRegPass.Password))
            {
                LblError.Text = "Todos los campos son obligatorios.";
                return;
            }

            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                await SupabaseService.UpdateClientName(TxtLicense.Text, TxtBusinessName.Text);
                SecurityService.RegisterAdmin(TxtLicense.Text, TxtRegUser.Text, TxtRegPass.Password);
                OpenMainWindow();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error registro nube: {ex.Message}");
                // Si falla la nube, intentamos entrar localmente igual
                SecurityService.RegisterAdmin(TxtLicense.Text, TxtRegUser.Text, TxtRegPass.Password);
                OpenMainWindow();
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        // --- MÉTODO CLAVE PARA EVITAR EL CRASH ---
        private void OpenMainWindow()
        {
            // 1. CREAR Y ACTIVAR LA VENTANA PRINCIPAL PRIMERO
            // Esto asegura que Windows tenga una ventana a la que "agarrarse"
            MainWindow mainWindow = new MainWindow();
            mainWindow.Activate();

            // 2. SOLO DESPUÉS CERRAMOS ESTA
            this.Close();
        }
    }
}