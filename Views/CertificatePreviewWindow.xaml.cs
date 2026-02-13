using Microsoft.UI.Xaml;
using OpticaPro.Models;
using OpticaPro.Services;
using System;

namespace OpticaPro.Views
{
    public sealed partial class CertificatePreviewWindow : Window
    {
        public CertificatePreviewWindow(CertificateData data)
        {
            this.InitializeComponent();
            try { this.AppWindow.Resize(new Windows.Graphics.SizeInt32(950, 950)); } catch { }
            LoadData(data);
        }

        private void LoadData(CertificateData data)
        {
            if (data == null) return;

            // Header
            RunClinicName.Text = string.IsNullOrEmpty(data.ClinicName) ? "OPTICA" : data.ClinicName;
            RunClinicAddress.Text = data.ClinicAddress;
            RunClinicPhone.Text = "Telf: " + data.ClinicPhone;
            RunClinicRuc.Text = "RUC: " + data.ClinicRuc;

            // Paciente
            RunPatientName.Text = data.PatientName?.ToUpper();
            RunPatientAge.Text = data.PatientAge;
            RunHistory.Text = data.PatientHistory;
            RunDate.Text = data.Date;

            // RX
            TxtSphOD.Text = data.SphereOD; TxtCylOD.Text = data.CylOD; TxtAxisOD.Text = data.AxisOD; TxtAvOD.Text = data.AvOD;
            TxtSphOI.Text = data.SphereOI; TxtCylOI.Text = data.CylOI; TxtAxisOI.Text = data.AxisOI; TxtAvOI.Text = data.AvOI;

            // Footer
            TxtDiag.Text = data.Diagnosis;
            TxtRec.Text = data.Recommendation;

            RunDoctorName.Text = data.DoctorName;
            RunDoctorSpecialty.Text = data.DoctorSpecialty;
            RunDoctorLicense.Text = "Reg: " + data.DoctorLicense;
        }

        private async void Print_Click(object sender, RoutedEventArgs e)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var printService = new PrintService(hWnd);
            try { await printService.PrintAsync(PrintArea); }
            catch { }
        }

        private void Close_Click(object sender, RoutedEventArgs e) => this.Close();
    }
}