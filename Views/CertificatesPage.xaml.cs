using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Linq;
using System.Globalization;
using System.Collections.ObjectModel;
using System.Collections.Generic;

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
            LoadPatients();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            SettingsService.Load();
        }

        private void LoadPatients()
        {
            PatientsListSource.Clear();
            var all = PatientRepository.GetAllPatients();
            foreach (var p in all.OrderBy(x => x.FullName))
            {
                PatientsListSource.Add(p);
            }
            PatientsList.ItemsSource = PatientsListSource;
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            var query = sender.Text.ToLower();
            var all = PatientRepository.GetAllPatients();
            var filtered = all.Where(p =>
                (p.FullName != null && p.FullName.ToLower().Contains(query)) ||
                (p.Dni != null && p.Dni.Contains(query))
            );

            PatientsListSource.Clear();
            foreach (var p in filtered) PatientsListSource.Add(p);
        }

        private void PatientsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (PatientsList.SelectedItem is Patient p)
            {
                TxtPatientName.Text = p.FullName ?? "";
                TxtAge.Text = p.Age > 0 ? $"{p.Age} años" : "";
                TxtHistory.Text = !string.IsNullOrEmpty(p.VisualHistory) ? p.VisualHistory : "Ninguno";
                ClearExamFields();

                var exams = PatientRepository.GetExamsByPatientId(p.Id);
                if ((exams == null || exams.Count == 0) && !string.IsNullOrEmpty(p.Dni))
                {
                    exams = PatientRepository.GetExamsByPatientId(p.Dni);
                }

                if (exams != null && exams.Count > 0)
                {
                    CmbExams.ItemsSource = exams;
                    CmbExams.SelectedIndex = 0;
                    InfoAutoFill.IsOpen = true;
                    InfoAutoFill.Message = "Se cargó el historial médico.";
                }
                else
                {
                    CmbExams.ItemsSource = null;
                    InfoAutoFill.IsOpen = false;
                }
            }
        }

        private void CmbExams_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (CmbExams.SelectedItem is ClinicalExam exam)
            {
                TxtSphereOd.Text = exam.SphereOD ?? ""; TxtSphereOi.Text = exam.SphereOI ?? "";
                TxtCylOd.Text = exam.CylOD ?? ""; TxtCylOi.Text = exam.CylOI ?? "";
                TxtAxisOd.Text = exam.AxisOD ?? ""; TxtAxisOi.Text = exam.AxisOI ?? "";
                TxtAddOd.Text = exam.AddOD ?? ""; TxtAddOi.Text = exam.AddOI ?? "";
                TxtAvOd.Text = exam.AvOD ?? ""; TxtAvOi.Text = exam.AvOI ?? "";

                TxtDiagnostico.Text = exam.DiagnosticoResumen ?? "";
                TxtRecomendacion.Text = exam.Observaciones ?? "";

                var partesAntecedentes = new List<string>();
                if (!string.IsNullOrEmpty(exam.AntecedentesSistemicos)) partesAntecedentes.Add($"Sistémicos: {exam.AntecedentesSistemicos}");
                if (!string.IsNullOrEmpty(exam.AntecedentesOculares)) partesAntecedentes.Add($"Oculares: {exam.AntecedentesOculares}");
                if (!string.IsNullOrEmpty(exam.HistoriaEnfermedad)) partesAntecedentes.Add($"Historia: {exam.HistoriaEnfermedad}");

                if (partesAntecedentes.Count > 0) TxtHistory.Text = string.Join(" | ", partesAntecedentes);
            }
        }

        private void ClearExamFields()
        {
            TxtSphereOd.Text = ""; TxtSphereOi.Text = "";
            TxtCylOd.Text = ""; TxtCylOi.Text = "";
            TxtAxisOd.Text = ""; TxtAxisOi.Text = "";
            TxtAddOd.Text = ""; TxtAddOi.Text = "";
            TxtAvOd.Text = ""; TxtAvOi.Text = "";
            TxtDiagnostico.Text = ""; TxtRecomendacion.Text = "";
        }

        private void PickDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args) { }

        private async void SaveOnly_Click(object sender, RoutedEventArgs e)
        {
            await new ContentDialog
            {
                Title = "Información",
                Content = "Los datos del formulario están listos.",
                CloseButtonText = "Entendido",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }

        // --- CORRECCIÓN AQUÍ ---
        private void Preview_Click(object sender, RoutedEventArgs e)
        {
            var s = SettingsService.Current;

            var data = new CertificateData
            {
                PatientName = TxtPatientName.Text,
                PatientAge = TxtAge.Text,
                PatientHistory = TxtHistory.Text,
                Date = PickDate.Date.HasValue ? PickDate.Date.Value.ToString("dd 'de' MMMM 'de' yyyy", new CultureInfo("es-ES")) : DateTime.Now.ToString("dd/MM/yyyy"),

                ClinicName = s?.ClinicName ?? "Nombre Clínica",
                ClinicAddress = s?.ClinicAddress ?? "",
                ClinicPhone = s?.ClinicPhone ?? "",
                ClinicRuc = s?.ClinicRuc ?? "",

                DoctorName = s?.DoctorName ?? "Dr. Desconocido",
                DoctorSpecialty = s?.DoctorSpecialty ?? "Optometría",
                DoctorLicense = s?.DoctorLicense ?? "",

                SphereOD = TxtSphereOd.Text,
                CylOD = TxtCylOd.Text,
                AxisOD = TxtAxisOd.Text,
                AddOD = TxtAddOd.Text,
                AvOD = TxtAvOd.Text,
                SphereOI = TxtSphereOi.Text,
                CylOI = TxtCylOi.Text,
                AxisOI = TxtAxisOi.Text,
                AddOI = TxtAddOi.Text,
                AvOI = TxtAvOi.Text,

                Diagnosis = TxtDiagnostico.Text,
                Recommendation = TxtRecomendacion.Text
            };

            // 1. Instanciar VENTANA VACÍA (Soluciona CS1729)
            var previewWindow = new CertificatePreviewWindow();

            // 2. Cargar datos manualmente
            previewWindow.LoadData(data);

            // 3. Mostrar
            previewWindow.Activate();
        }
    }
}