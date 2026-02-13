using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.ObjectModel;

namespace OpticaPro.Views
{
    public sealed partial class CertificatesPage : Page
    {
        public ObservableCollection<Patient> PatientsListSource { get; set; } = new ObservableCollection<Patient>();

        public CertificatesPage()
        {
            this.InitializeComponent();
            CultureInfo.CurrentUICulture = new CultureInfo("es-ES");
            PickDate.Date = DateTimeOffset.Now;
            UpdateDateText();
            LoadPatients();
        }

        // AL ENTRAR A LA PÁGINA: Forzamos la carga de ajustes
        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SettingsService.Load();
        }

        private void LoadPatients()
        {
            PatientsListSource.Clear();
            var all = PatientRepository.GetAllPatients();
            foreach (var p in all) PatientsListSource.Add(p);
            PatientsList.ItemsSource = PatientsListSource;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var query = sender.Text.ToLower();
            var all = PatientRepository.GetAllPatients();
            var filtered = all.Where(p => p.FullName.ToLower().Contains(query) || p.Dni.Contains(query));
            PatientsListSource.Clear();
            foreach (var p in filtered) PatientsListSource.Add(p);
        }

        private void PatientsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientsList.SelectedItem is Patient p)
            {
                TxtPatientName.Text = p.FullName;
                TxtAge.Text = $"{p.Age} años";
                TxtHistory.Text = !string.IsNullOrEmpty(p.VisualHistory) ? p.VisualHistory : "Ninguno";

                if (p.ExamHistory != null && p.ExamHistory.Count > 0)
                {
                    var lastExam = p.ExamHistory.Last();
                    TxtSphereOd.Text = lastExam.SphereOD; TxtSphereOi.Text = lastExam.SphereOI;
                    TxtCylOd.Text = lastExam.CylOD; TxtCylOi.Text = lastExam.CylOI;
                    TxtAxisOd.Text = lastExam.AxisOD; TxtAxisOi.Text = lastExam.AxisOI;
                    TxtAvOd.Text = lastExam.AvOD; TxtAvOi.Text = lastExam.AvOI;
                    TxtDiagnostico.Text = lastExam.DiagnosticoResumen;
                    InfoAutoFill.IsOpen = true;
                }
                else
                {
                    TxtSphereOd.Text = ""; TxtSphereOi.Text = "";
                    TxtCylOd.Text = ""; TxtCylOi.Text = "";
                    TxtAxisOd.Text = ""; TxtAxisOi.Text = "";
                    TxtAvOd.Text = ""; TxtAvOi.Text = "";
                    InfoAutoFill.IsOpen = false;
                }
            }
        }

        private void PickDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            UpdateDateText();
        }

        private void UpdateDateText()
        {
            // Lógica auxiliar si necesitas mostrar fecha en texto
        }

        private async void SaveOnly_Click(object sender, RoutedEventArgs e)
        {
            await new ContentDialog { Title = "Guardado", Content = "Registro archivado.", CloseButtonText = "Ok", XamlRoot = this.XamlRoot }.ShowAsync();
        }

        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            // LEER DATOS REALES DE AJUSTES
            var s = SettingsService.Current;

            var data = new CertificateData
            {
                PatientName = TxtPatientName.Text,
                PatientAge = TxtAge.Text,
                PatientHistory = TxtHistory.Text,
                Date = PickDate.Date.HasValue ? PickDate.Date.Value.ToString("dd 'de' MMMM 'de' yyyy", new CultureInfo("es-ES")) : "-",

                // DATOS REALES DE LA CLÍNICA
                ClinicName = s.ClinicName,
                ClinicAddress = s.ClinicAddress,
                ClinicPhone = s.ClinicPhone,
                ClinicRuc = s.ClinicRuc,

                // DATOS REALES DEL DOCTOR
                DoctorName = s.DoctorName,
                DoctorSpecialty = s.DoctorSpecialty,
                DoctorLicense = s.DoctorLicense,

                SphereOD = TxtSphereOd.Text,
                CylOD = TxtCylOd.Text,
                AxisOD = TxtAxisOd.Text,
                AvOD = TxtAvOd.Text,
                SphereOI = TxtSphereOi.Text,
                CylOI = TxtCylOi.Text,
                AxisOI = TxtAxisOi.Text,
                AvOI = TxtAvOi.Text,
                Diagnosis = TxtDiagnostico.Text,
                Recommendation = TxtRecomendacion.Text
            };

            // ABRIR VENTANA DE IMPRESIÓN
            var previewWindow = new CertificatePreviewWindow(data);
            previewWindow.Activate();
        }
    }
}