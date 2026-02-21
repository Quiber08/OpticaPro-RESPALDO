using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Services;
using System;
using System.Threading.Tasks;

namespace OpticaPro.Views
{
    public sealed partial class SettingsPage : Page
    {
        private bool _isLoaded = false;

        public SettingsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (MainWindow.Current != null)
            {
                // 1. Sincronizar Datos Usuario
                string nombreActual = MainWindow.Current.SidebarProfileName.Text;
                if (!string.IsNullOrEmpty(nombreActual))
                {
                    BigProfileName.Text = nombreActual.ToUpper();
                    BigProfilePic.DisplayName = nombreActual;
                    var parts = nombreActual.Split(' ');
                    if (parts.Length > 0) BigProfilePic.Initials = parts[0].Substring(0, 1).ToUpper();
                }

                if (MainWindow.Current.SidebarProfilePic.ProfilePicture != null)
                {
                    BigProfilePic.ProfilePicture = MainWindow.Current.SidebarProfilePic.ProfilePicture;
                }

                // 2. Sincronizar Tema
                if (MainWindow.Current.Content is FrameworkElement root)
                {
                    switch (root.RequestedTheme)
                    {
                        case ElementTheme.Light: ThemeBox.SelectedIndex = 0; break;
                        case ElementTheme.Dark: ThemeBox.SelectedIndex = 1; break;
                        case ElementTheme.Default: ThemeBox.SelectedIndex = 2; break;
                    }
                }
            }
            _isLoaded = true;
        }

        private void Theme_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoaded && MainWindow.Current.Content is FrameworkElement root)
            {
                ElementTheme selectedTheme = ElementTheme.Default;

                switch (ThemeBox.SelectedIndex)
                {
                    case 0: selectedTheme = ElementTheme.Light; break;
                    case 1: selectedTheme = ElementTheme.Dark; break;
                    case 2: selectedTheme = ElementTheme.Default; break;
                }

                root.RequestedTheme = selectedTheme;
                MainWindow.Current.SetTitleBarTheme(selectedTheme);
            }
        }

        private void EditAccount_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(AccountPage));
        private void OpenBackup_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(BackupPage));
        private void EditClinic_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(ClinicPage));
        private void EditProfessional_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(ProfessionalPage));
        private void ManageUsers_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(UsersPage));

        private async void CheckUpdates_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog loading = new ContentDialog
            {
                Title = "Buscando...",
                Content = new ProgressRing { IsActive = true },
                XamlRoot = this.XamlRoot
            };
            var t = loading.ShowAsync();

            var updateInfo = await SupabaseService.CheckForUpdates();

            loading.Hide();

            string currentVersion = "1.0.0";

            if (updateInfo != null && updateInfo.Version != currentVersion)
            {
                // --- CORRECCIÓN DEFINITIVA ---
                // Ahora usamos .Notes porque así lo definimos en el Modelo que conecta con SQL
                ContentDialog dialog = new ContentDialog
                {
                    Title = $"¡Nueva Versión {updateInfo.Version} Disponible!",
                    Content = $"Notas de la versión:\n{updateInfo.Notes}\n\n¿Deseas descargarla?",
                    PrimaryButtonText = "Ir a Descarga",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                var result = await dialog.ShowAsync();

                if (result == ContentDialogResult.Primary)
                {
                    if (!string.IsNullOrEmpty(updateInfo.DownloadUrl))
                    {
                        await Windows.System.Launcher.LaunchUriAsync(new Uri(updateInfo.DownloadUrl));
                    }
                }
            }
            else
            {
                await new ContentDialog
                {
                    Title = "Todo al día",
                    Content = $"Tienes la última versión instalada ({currentVersion}).",
                    CloseButtonText = "Genial",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }
    }
}