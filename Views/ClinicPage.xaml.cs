using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Services;
using System;

namespace OpticaPro.Views
{
    public sealed partial class ClinicPage : Page
    {
        public ClinicPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            // 1. Cargar datos desde SQLite
            SettingsService.Load();
            var s = SettingsService.Current;

            // 2. Rellenar los campos
            TxtName.Text = s.ClinicName ?? "";
            TxtRuc.Text = s.ClinicRuc ?? "";
            TxtAddress.Text = s.ClinicAddress ?? "";
            TxtCity.Text = s.ClinicCity ?? "";
            TxtPhone.Text = s.ClinicPhone ?? "";
            TxtEmail.Text = s.ClinicEmail ?? "";
            TxtSlogan.Text = s.ClinicSlogan ?? "";
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Guardar en SQLite (Base de datos local)
                var s = SettingsService.Current;
                s.ClinicName = TxtName.Text;
                s.ClinicRuc = TxtRuc.Text;
                s.ClinicAddress = TxtAddress.Text;
                s.ClinicCity = TxtCity.Text;
                s.ClinicPhone = TxtPhone.Text;
                s.ClinicEmail = TxtEmail.Text;
                s.ClinicSlogan = TxtSlogan.Text;

                SettingsService.Save(); // ¡Esto ahora guarda en .db3!

                // 2. Sincronizar nombre con la Nube (Supabase)
                // Esto es útil para tu licencia, pero no afecta al certificado local
                try
                {
                    string currentLicense = SecurityService.GetLicenseKey();
                    if (!string.IsNullOrEmpty(currentLicense))
                    {
                        await SupabaseService.UpdateClientName(currentLicense, TxtName.Text);
                    }
                }
                catch { /* Ignoramos errores de red */ }

                // 3. Confirmación
                await new ContentDialog
                {
                    Title = "Guardado Exitoso",
                    Content = "La información de la clínica se ha guardado en la base de datos.",
                    CloseButtonText = "Entendido",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                GoBackSafe();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = $"No se pudo guardar: {ex.Message}",
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => GoBackSafe();

        private void GoBackSafe()
        {
            if (Frame.CanGoBack) Frame.GoBack();
            // Asegúrate de que SettingsPage exista y sea accesible
            // else Frame.Navigate(typeof(SettingsPage)); 
        }
    }
}