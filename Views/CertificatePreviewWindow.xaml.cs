using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using Windows.Storage.Pickers; // Necesario para guardar archivos

namespace OpticaPro.Views
{
    public sealed partial class CertificatePreviewWindow : Window
    {
        // Variable para guardar los datos y usarlos al generar el PDF
        private CertificateData _currentData;

        public CertificatePreviewWindow()
        {
            this.InitializeComponent();
            try { this.AppWindow.Resize(new Windows.Graphics.SizeInt32(900, 1000)); } catch { }
        }

        public void LoadData(CertificateData data)
        {
            if (data == null) return;

            // 1. Guardamos los datos en la variable privada para usarlos al guardar
            _currentData = data;

            // 2. Llenamos la vista previa en pantalla (XAML)
            RunClinicName.Text = !string.IsNullOrEmpty(data.ClinicName) ? data.ClinicName : "OPTICA";
            RunClinicAddress.Text = data.ClinicAddress;
            RunClinicPhone.Text = "Telf: " + data.ClinicPhone;
            RunClinicRuc.Text = "RUC: " + data.ClinicRuc;

            RunPatientName.Text = data.PatientName?.ToUpper();
            RunPatientAge.Text = data.PatientAge;
            RunHistory.Text = data.PatientHistory;
            RunDate.Text = data.Date;

            TxtSphOD.Text = data.SphereOD;
            TxtCylOD.Text = data.CylOD;
            TxtAxisOD.Text = data.AxisOD;
            TxtAddOD.Text = data.AddOD;
            TxtAvOD.Text = data.AvOD;

            TxtSphOI.Text = data.SphereOI;
            TxtCylOI.Text = data.CylOI;
            TxtAxisOI.Text = data.AxisOI;
            TxtAddOI.Text = data.AddOI;
            TxtAvOI.Text = data.AvOI;

            TxtDiag.Text = data.Diagnosis;
            TxtRec.Text = data.Recommendation;

            RunDoctorName.Text = data.DoctorName;
            RunDoctorSpecialty.Text = data.DoctorSpecialty;
            RunDoctorLicense.Text = "Reg: " + data.DoctorLicense;
        }

        private async void SavePdf_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as Button;
            if (btn != null) btn.IsEnabled = false;

            try
            {
                if (_currentData == null)
                {
                    ShowError("No hay datos cargados para generar el certificado.");
                    return;
                }

                // 1. Configurar el diálogo para guardar archivo
                var savePicker = new FileSavePicker();
                savePicker.SuggestedStartLocation = PickerLocationId.DocumentsLibrary;
                savePicker.FileTypeChoices.Add("Documento PDF", new List<string>() { ".pdf" });
                savePicker.SuggestedFileName = $"Certificado_{_currentData.PatientName.Replace(" ", "_")}";

                // -- Truco para que el Picker funcione en WinUI 3 --
                var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
                WinRT.Interop.InitializeWithWindow.Initialize(savePicker, hWnd);

                // 2. Mostrar diálogo y esperar a que el usuario elija ruta
                var file = await savePicker.PickSaveFileAsync();

                if (file != null)
                {
                    // 3. Generar el PDF usando el nuevo PrintService
                    var printService = new PrintService();
                    printService.GenerateCertificate(_currentData, file.Path);

                    // 4. (Opcional) Abrir el PDF automáticamente
                    await Windows.System.Launcher.LaunchFileAsync(file);
                }
            }
            catch (Exception ex)
            {
                ShowError($"Error al guardar el PDF:\n{ex.Message}");
            }
            finally
            {
                if (btn != null) btn.IsEnabled = true;
            }
        }

        private async void ShowError(string message)
        {
            var dialog = new ContentDialog
            {
                Title = "Información",
                Content = message,
                CloseButtonText = "Ok",
                XamlRoot = this.Content.XamlRoot
            };
            await dialog.ShowAsync();
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}