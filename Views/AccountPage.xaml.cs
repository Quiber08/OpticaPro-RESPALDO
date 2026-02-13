using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media.Imaging;
using OpticaPro.Services;
using System;
using System.IO;
using Windows.Storage.Pickers;

namespace OpticaPro.Views
{
    public sealed partial class AccountPage : Page
    {
        private string _tempImagePath = null;

        public AccountPage()
        {
            this.InitializeComponent();
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Cargar Nombre
                TxtUserNameInput.Text = SecurityService.CurrentUserName;

                // Cargar Foto
                UpdateProfilePicDisplay(SecurityService.CurrentProfileImage, SecurityService.CurrentUserName);
            }
            catch { }
        }

        // --- CAMBIO DE FOTO ---
        private async void BtnChangePhoto_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var openPicker = new FileOpenPicker();

                // Configurar ventana padre (necesario en WinUI 3)
                var window = MainWindow.Current;
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(window);
                WinRT.Interop.InitializeWithWindow.Initialize(openPicker, hWnd);

                openPicker.ViewMode = PickerViewMode.Thumbnail;
                openPicker.SuggestedStartLocation = PickerLocationId.PicturesLibrary;
                openPicker.FileTypeFilter.Add(".jpg");
                openPicker.FileTypeFilter.Add(".jpeg");
                openPicker.FileTypeFilter.Add(".png");

                var file = await openPicker.PickSingleFileAsync();
                if (file != null)
                {
                    // Guardar en carpeta local de la app
                    string appFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpticaProData");
                    if (!Directory.Exists(appFolder)) Directory.CreateDirectory(appFolder);

                    // Nombre único para evitar caché
                    string destFile = Path.Combine(appFolder, $"profile_{DateTime.Now.Ticks}{Path.GetExtension(file.Path)}");

                    File.Copy(file.Path, destFile, true);

                    _tempImagePath = destFile; // Guardamos ruta temporal

                    // Actualizamos vista previa
                    UpdateProfilePicDisplay(_tempImagePath, TxtUserNameInput.Text);
                }
            }
            catch (Exception ex)
            {
                ShowMessage("Error de Imagen", "No se pudo cargar la foto: " + ex.Message);
            }
        }

        private void UpdateProfilePicDisplay(string path, string name)
        {
            ProfilePicPreview.DisplayName = name;

            if (!string.IsNullOrEmpty(path) && File.Exists(path))
            {
                try
                {
                    ProfilePicPreview.ProfilePicture = new BitmapImage(new Uri(path));
                }
                catch
                {
                    ProfilePicPreview.ProfilePicture = null;
                }
            }
            else
            {
                // Si no hay foto, usa iniciales
                ProfilePicPreview.ProfilePicture = null;
                if (!string.IsNullOrEmpty(name))
                {
                    var parts = name.Split(' ');
                    string initials = parts[0].Substring(0, 1).ToUpper();
                    if (parts.Length > 1) initials += parts[1].Substring(0, 1).ToUpper();
                    ProfilePicPreview.Initials = initials;
                }
            }
        }

        // --- GUARDAR PERFIL (NOMBRE Y FOTO) ---
        private async void BtnSaveProfile_Click(object sender, RoutedEventArgs e)
        {
            string newName = TxtUserNameInput.Text.Trim();
            if (string.IsNullOrEmpty(newName))
            {
                ShowMessage("Atención", "El nombre no puede estar vacío.");
                return;
            }

            // Usamos la nueva foto si eligió una, o mantenemos la actual
            string finalImage = _tempImagePath ?? SecurityService.CurrentProfileImage;

            // Guardar y notificar a MainWindow
            SecurityService.UpdateProfile(newName, finalImage);

            ShowMessage("Perfil Actualizado", "Tu nombre y foto se han guardado correctamente.");
        }

        // --- CAMBIAR CONTRASEÑA ---
        private async void BtnChangePass_Click(object sender, RoutedEventArgs e)
        {
            string current = PbCurrent.Password;
            string newP = PbNew.Password;
            string confirm = PbConfirm.Password;

            if (string.IsNullOrEmpty(current))
            {
                ShowMessage("Seguridad", "Debes ingresar tu contraseña actual para hacer cambios.");
                return;
            }

            if (string.IsNullOrEmpty(newP))
            {
                ShowMessage("Error", "La nueva contraseña no puede estar vacía.");
                return;
            }

            if (newP != confirm)
            {
                ShowMessage("Error", "Las nuevas contraseñas no coinciden.");
                return;
            }

            bool success = SecurityService.ChangePassword(current, newP);

            if (success)
            {
                PbCurrent.Password = "";
                PbNew.Password = "";
                PbConfirm.Password = "";
                ShowMessage("Éxito", "Tu contraseña ha sido actualizada.");
            }
            else
            {
                ShowMessage("Error", "La contraseña actual es incorrecta.");
            }
        }

        private async void ShowMessage(string title, string content)
        {
            var dialog = new ContentDialog
            {
                Title = title,
                Content = content,
                CloseButtonText = "Entendido",
                XamlRoot = this.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }
}