using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using OpticaPro.Services;
using OpticaPro.Models;
using System;
using System.Collections.Generic;
using Windows.UI;

namespace OpticaPro.Views
{
    public class UserDisplayModel
    {
        public string Username { get; set; }
        public string FullName { get; set; }
        public string Role { get; set; }
        public bool IsAdmin { get; set; }
    }

    public sealed partial class LoginWindow : Window
    {
        private UserDisplayModel _selectedUser;

        public LoginWindow()
        {
            this.InitializeComponent();
            try
            {
                this.AppWindow.Resize(new Windows.Graphics.SizeInt32(1050, 750));
            }
            catch { }
            CheckSystemStatus();
        }

        private void CheckSystemStatus()
        {
            if (SecurityService.IsAppInitialized())
            {
                ShowUserSelection();
            }
            else
            {
                ShowLicensePanel();
            }
        }

        private void ShowUserSelection()
        {
            PanelLicense.Visibility = Visibility.Collapsed;
            PanelRegister.Visibility = Visibility.Collapsed;
            PanelPassword.Visibility = Visibility.Collapsed;
            if (PanelRecovery != null) PanelRecovery.Visibility = Visibility.Collapsed;

            UserSelectionList.Visibility = Visibility.Visible;
            LblWelcome.Text = "¡Bienvenido de nuevo!";
            LblInstruction.Text = "Selecciona tu usuario para acceder al sistema.";
            LblError.Text = "";
            LoadUsers();
        }

        private void ShowLicensePanel()
        {
            PanelLicense.Visibility = Visibility.Visible;
            PanelRegister.Visibility = Visibility.Collapsed;
            UserSelectionList.Visibility = Visibility.Collapsed;
            PanelPassword.Visibility = Visibility.Collapsed;
            LblWelcome.Text = "Activación de Sistema";
            LblInstruction.Text = "Este equipo requiere una licencia válida.";
        }

        private void ShowRegisterPanel()
        {
            PanelRegister.Visibility = Visibility.Visible;
            PanelLicense.Visibility = Visibility.Collapsed;
            LblWelcome.Text = "Registro de Administrador";
            LblInstruction.Text = "Configura la cuenta principal del sistema.";
        }

        private void ShowPasswordPanel(UserDisplayModel user)
        {
            _selectedUser = user;
            SelectedUserName.Text = user.FullName;
            SelectedUserPic.DisplayName = user.FullName;

            UserSelectionList.Visibility = Visibility.Collapsed;
            PanelPassword.Visibility = Visibility.Visible;

            LblWelcome.Text = $"Hola, {user.FullName.Split(' ')[0]}";
            LblInstruction.Text = "Introduce tu contraseña para continuar.";
            LblError.Text = "";

            TxtLoginPass.Password = "";
            TxtLoginPass.Focus(FocusState.Programmatic);
        }

        private void LoadUsers()
        {
            var displayList = new List<UserDisplayModel>();
            displayList.Add(new UserDisplayModel
            {
                Username = SecurityService.GetAdminName(),
                FullName = "Administrador",
                Role = "Gerente / Dueño",
                IsAdmin = true
            });

            var employees = UserRepository.GetAllUsers();
            if (employees != null)
            {
                foreach (var emp in employees)
                {
                    displayList.Add(new UserDisplayModel
                    {
                        Username = emp.Username,
                        FullName = emp.FullName,
                        Role = emp.Role,
                        IsAdmin = false
                    });
                }
            }
            UserSelectionList.ItemsSource = displayList;
        }

        private void UserItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is UserDisplayModel user)
            {
                ShowPasswordPanel(user);
            }
        }

        private void BackToUsers_Click(object sender, RoutedEventArgs e) => ShowUserSelection();

        private void TxtLoginPass_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) Login_Click(sender, e);
        }

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
                    ShowError("Contraseña incorrecta. Inténtalo de nuevo.");
                    TxtLoginPass.Password = "";
                    TxtLoginPass.Focus(FocusState.Programmatic);
                }
            }
            catch (Exception ex)
            {
                ShowError("Error: " + ex.Message);
            }
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
                TextBox txtLicenseVerify = new TextBox
                {
                    Header = "Licencia del Producto",
                    PlaceholderText = "Ingresa tu clave de activación",
                    FontFamily = new FontFamily("Consolas")
                };
                PasswordBox txtNewPass = new PasswordBox
                {
                    Header = "Nueva Contraseña",
                    PlaceholderText = "Escribe tu nueva clave maestra"
                };

                StackPanel panel = new StackPanel { Spacing = 15 };
                panel.Children.Add(new TextBlock
                {
                    Text = "Por seguridad, verifica tu identidad ingresando la licencia original.",
                    Foreground = new SolidColorBrush(Microsoft.UI.Colors.Gray),
                    TextWrapping = TextWrapping.Wrap
                });
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
                            var successDialog = new ContentDialog
                            {
                                Title = "¡Éxito!",
                                Content = "Contraseña actualizada. Úsala para ingresar.",
                                CloseButtonText = "Ok",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await successDialog.ShowAsync();
                            TxtLoginPass.Password = "";
                        }
                        else
                        {
                            var errorDialog = new ContentDialog
                            {
                                Title = "Error",
                                Content = "La contraseña no puede estar vacía.",
                                CloseButtonText = "Ok",
                                XamlRoot = this.Content.XamlRoot
                            };
                            await errorDialog.ShowAsync();
                        }
                    }
                    else
                    {
                        var failDialog = new ContentDialog
                        {
                            Title = "Error",
                            Content = "Licencia incorrecta. No se pudo verificar la identidad.",
                            CloseButtonText = "Cerrar",
                            XamlRoot = this.Content.XamlRoot
                        };
                        await failDialog.ShowAsync();
                    }
                }
            }
        }

        private void BackToLogin_Click(object sender, RoutedEventArgs e) { }
        private void ConfirmRecovery_Click(object sender, RoutedEventArgs e) { }

        private async void ValidateLicense_Click(object sender, RoutedEventArgs e)
        {
            TxtLicense.IsEnabled = false;
            LblError.Text = "Validando licencia...";
            LblError.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 255, 255));

            try
            {
                var result = await SupabaseService.ValidateAndRegisterLicense(TxtLicense.Text);

                if (result.isValid)
                {
                    LblError.Text = "";
                    ShowRegisterPanel();
                    if (!string.IsNullOrEmpty(result.clientName))
                    {
                        TxtBusinessName.Text = result.clientName;
                    }
                }
                else
                {
                    ShowError(result.message);
                }
            }
            catch (Exception ex)
            {
                ShowError("Error de conexión: " + ex.Message);
            }
            finally
            {
                TxtLicense.IsEnabled = true;
            }
        }

        private async void Register_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(TxtRegUser.Text) || string.IsNullOrWhiteSpace(TxtRegPass.Password))
            {
                ShowError("Todos los campos son obligatorios.");
                return;
            }

            try
            {
                await SupabaseService.UpdateClientName(TxtLicense.Text, TxtBusinessName.Text);
                SecurityService.RegisterAdmin(TxtLicense.Text, TxtRegUser.Text, TxtRegPass.Password);
                OpenMainWindow();
            }
            catch (Exception ex)
            {
                try
                {
                    SecurityService.RegisterAdmin(TxtLicense.Text, TxtRegUser.Text, TxtRegPass.Password);
                    OpenMainWindow();
                }
                catch
                {
                    ShowError("Error al registrar: " + ex.Message);
                }
            }
        }

        // --- UTILIDADES ---

        private void OpenMainWindow()
        {
            MainWindow mainWindow = new MainWindow();

            // --- CORRECCIÓN CLAVE: ASIGNAR LA NUEVA VENTANA ---
            App.m_window = mainWindow;
            // --------------------------------------------------

            mainWindow.Activate();
            this.Close();
        }

        private void ShowError(string message)
        {
            LblError.Foreground = new SolidColorBrush(Color.FromArgb(255, 255, 107, 107));
            LblError.Text = message;
        }
    }
}