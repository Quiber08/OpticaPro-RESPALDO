using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Services;
using System;

namespace OpticaPro.Views
{
    public sealed partial class ProfessionalPage : Page
    {
        public ProfessionalPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                // Cargar desde SQLite
                SettingsService.Load();
                var s = SettingsService.Current;

                TxtName.Text = s.DoctorName ?? "";
                TxtSpecialty.Text = s.DoctorSpecialty ?? "";
                TxtLicense.Text = s.DoctorLicense ?? "";
            }
            catch (Exception ex)
            {
                // Si falla la carga, mostramos un aviso pero no cerramos la app
                System.Diagnostics.Debug.WriteLine("Error cargando profesional: " + ex.Message);
            }
        }

        private async void Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // 1. Actualizar objeto en memoria
                var s = SettingsService.Current;
                s.DoctorName = TxtName.Text;
                s.DoctorSpecialty = TxtSpecialty.Text;
                s.DoctorLicense = TxtLicense.Text;

                // 2. Guardar en SQLite (Persistencia Real)
                SettingsService.Save();

                // 3. Confirmación Visual
                await new ContentDialog
                {
                    Title = "Guardado Exitoso",
                    Content = "Los datos del profesional se han actualizado correctamente en la base de datos.",
                    CloseButtonText = "Aceptar",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();

                GoBackSafe();
            }
            catch (Exception ex)
            {
                await new ContentDialog
                {
                    Title = "Error",
                    Content = $"Hubo un problema al guardar: {ex.Message}",
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            GoBackSafe();
        }

        private void GoBackSafe()
        {
            if (Frame.CanGoBack)
            {
                Frame.GoBack();
            }
            else
            {
                // Si no hay historial, vamos a SettingsPage por defecto
                Frame.Navigate(typeof(SettingsPage));
            }
        }
    }
}