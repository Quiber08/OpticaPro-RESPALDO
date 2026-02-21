using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Text;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

namespace OpticaPro.Views
{
    public sealed partial class ClinicalExamPage : Window
    {
        private Patient _currentPatient;
        private ClinicalExam _examToEdit;

        public ClinicalExamPage(Patient patient, ClinicalExam examToEdit = null)
        {
            this.InitializeComponent();
            _currentPatient = patient;
            _examToEdit = examToEdit;
            InitializeForm();
        }

        private void InitializeForm()
        {
            if (_currentPatient != null)
                PatientNameTitle.Text = $"PACIENTE: {_currentPatient.FullName}";

            TxtExamDate.Text = DateTime.Now.ToString("dd/MM/yyyy");

            if (_examToEdit != null) LoadExistingData();
        }

        private void LoadExistingData()
        {
            // 1. ANAMNESIS
            TxtMotivo.Text = _examToEdit.MotivoConsulta ?? "";
            TxtHistoriaActual.Text = _examToEdit.HistoriaEnfermedad ?? "";
            TxtMedication.Text = _examToEdit.MedicacionActual ?? "";

            // Lensometría
            TxtLensoSphOD.Text = _examToEdit.LensoSphOD; TxtLensoCylOD.Text = _examToEdit.LensoCylOD; TxtLensoAxisOD.Text = _examToEdit.LensoAxisOD; TxtLensoAddOD.Text = _examToEdit.LensoAddOD;
            TxtLensoSphOI.Text = _examToEdit.LensoSphOI; TxtLensoCylOI.Text = _examToEdit.LensoCylOI; TxtLensoAxisOI.Text = _examToEdit.LensoAxisOI; TxtLensoAddOI.Text = _examToEdit.LensoAddOI;
            TxtTipoLenteUso.Text = _examToEdit.LensoTipoLente;

            // Antecedentes (convertir string guardado a checkboxes activos)
            CheckFromString(_examToEdit.AntecedentesSistemicos, ChkDiabetes, ChkHipertension, ChkColesterol, ChkTiroides, ChkAlergias);
            CheckFromString(_examToEdit.AntecedentesOculares, ChkGlaucomaFam, ChkCatarataFam, ChkCirugiaRef, ChkTrauma, ChkEstrabismoFam);

            // 2. REFRACCIÓN
            TxtSphereOD.Text = _examToEdit.SphereOD; TxtCylOD.Text = _examToEdit.CylOD; TxtAxisOD.Text = _examToEdit.AxisOD; TxtAddOD.Text = _examToEdit.AddOD;
            TxtSphereOI.Text = _examToEdit.SphereOI; TxtCylOI.Text = _examToEdit.CylOI; TxtAxisOI.Text = _examToEdit.AxisOI; TxtAddOI.Text = _examToEdit.AddOI;
            TxtAvOD.Text = _examToEdit.AvOD; TxtAvOI.Text = _examToEdit.AvOI;
            TxtAvscOD.Text = _examToEdit.AvscOD; TxtAvscOI.Text = _examToEdit.AvscOI;
            TxtK1OD.Text = _examToEdit.K1OD; TxtK2OD.Text = _examToEdit.K2OD; TxtKAxisOD.Text = _examToEdit.KAxisOD;
            TxtK1OI.Text = _examToEdit.K1OI; TxtK2OI.Text = _examToEdit.K2OI; TxtKAxisOI.Text = _examToEdit.KAxisOI;

            if (double.TryParse(_examToEdit.Dip, NumberStyles.Any, CultureInfo.InvariantCulture, out double dp)) NbDP.Value = dp;

            // 3. SALUD OCULAR
            ChkPterigionOD.IsChecked = _examToEdit.HasPterygiumOD; ChkPterigionOI.IsChecked = _examToEdit.HasPterygiumOI;
            ChkCatarataOD.IsChecked = _examToEdit.HasCataractOD; ChkCatarataOI.IsChecked = _examToEdit.HasCataractOI;
            ChkGlaucomaOD.IsChecked = _examToEdit.HasGlaucomaSuspicionOD; ChkGlaucomaOI.IsChecked = _examToEdit.HasGlaucomaSuspicionOI;

            // Fondo de Ojo
            TxtFondoExcavacion.Text = _examToEdit.FondoExcavacion ?? "";
            TxtFondoMacula.Text = _examToEdit.FondoMacula ?? "";
            TxtFondoRetina.Text = _examToEdit.FondoRetina ?? "";

            // 4. DIAGNÓSTICO Y PLAN
            CheckFromString(_examToEdit.DiagnosticoResumen, ChkMiopia, ChkHipermetropia, ChkAstigmatismo, ChkPresbicia, ChkEmetropia);
            CheckFromString(_examToEdit.PatologiaBinocular, ChkAmbliopia, ChkEstrabismo, ChkConjuntivitis, ChkBlefaritis, ChkOjoSeco, ChkQueratocono);

            TxtObs.Text = _examToEdit.Observaciones ?? "";

            // Sugerencias
            SetComboValue(CmbDiseno, _examToEdit.SugerenciaDiseno);
            SetComboValue(CmbMaterial, _examToEdit.SugerenciaMaterial);
        }

        private async void SaveExam_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var exam = _examToEdit ?? new ClinicalExam();
                exam.PatientId = _currentPatient.Id;
                exam.Date = _examToEdit?.Date ?? DateTime.Now.ToString("dd/MM/yyyy HH:mm");

                // --- 1. GUARDAR ANAMNESIS ---
                exam.MotivoConsulta = TxtMotivo.Text;
                exam.HistoriaEnfermedad = TxtHistoriaActual.Text;
                exam.MedicacionActual = TxtMedication.Text;

                // Lensometría
                exam.LensoSphOD = TxtLensoSphOD.Text; exam.LensoCylOD = TxtLensoCylOD.Text; exam.LensoAxisOD = TxtLensoAxisOD.Text; exam.LensoAddOD = TxtLensoAddOD.Text;
                exam.LensoSphOI = TxtLensoSphOI.Text; exam.LensoCylOI = TxtLensoCylOI.Text; exam.LensoAxisOI = TxtLensoAxisOI.Text; exam.LensoAddOI = TxtLensoAddOI.Text;
                exam.LensoTipoLente = TxtTipoLenteUso.Text;

                // Checkboxes a String (Antecedentes)
                exam.AntecedentesSistemicos = GetCheckedString(ChkDiabetes, ChkHipertension, ChkColesterol, ChkTiroides, ChkAlergias);
                exam.AntecedentesOculares = GetCheckedString(ChkGlaucomaFam, ChkCatarataFam, ChkCirugiaRef, ChkTrauma, ChkEstrabismoFam);

                // --- 2. GUARDAR REFRACCIÓN ---
                exam.SphereOD = TxtSphereOD.Text; exam.CylOD = TxtCylOD.Text; exam.AxisOD = TxtAxisOD.Text; exam.AddOD = TxtAddOD.Text;
                exam.SphereOI = TxtSphereOI.Text; exam.CylOI = TxtCylOI.Text; exam.AxisOI = TxtAxisOI.Text; exam.AddOI = TxtAddOI.Text;
                exam.AvOD = TxtAvOD.Text; exam.AvOI = TxtAvOI.Text;
                exam.AvscOD = TxtAvscOD.Text; exam.AvscOI = TxtAvscOI.Text;
                exam.K1OD = TxtK1OD.Text; exam.K2OD = TxtK2OD.Text; exam.KAxisOD = TxtKAxisOD.Text;
                exam.K1OI = TxtK1OI.Text; exam.K2OI = TxtK2OI.Text; exam.KAxisOI = TxtKAxisOI.Text;
                exam.Dip = NbDP.Value.ToString();

                // --- 3. GUARDAR SALUD OCULAR ---
                exam.HasPterygiumOD = ChkPterigionOD.IsChecked ?? false; exam.HasPterygiumOI = ChkPterigionOI.IsChecked ?? false;
                exam.HasCataractOD = ChkCatarataOD.IsChecked ?? false; exam.HasCataractOI = ChkCatarataOI.IsChecked ?? false;
                exam.HasGlaucomaSuspicionOD = ChkGlaucomaOD.IsChecked ?? false; exam.HasGlaucomaSuspicionOI = ChkGlaucomaOI.IsChecked ?? false;

                // Fondo de Ojo
                exam.FondoExcavacion = TxtFondoExcavacion.Text;
                exam.FondoMacula = TxtFondoMacula.Text;
                exam.FondoRetina = TxtFondoRetina.Text;

                // --- 4. GUARDAR DIAGNÓSTICO Y PLAN ---
                exam.DiagnosticoResumen = GetCheckedString(ChkMiopia, ChkHipermetropia, ChkAstigmatismo, ChkPresbicia, ChkEmetropia);
                exam.PatologiaBinocular = GetCheckedString(ChkAmbliopia, ChkEstrabismo, ChkConjuntivitis, ChkBlefaritis, ChkOjoSeco, ChkQueratocono);
                exam.Observaciones = TxtObs.Text;

                // Sugerencias
                exam.SugerenciaDiseno = (CmbDiseno.SelectedItem as ComboBoxItem)?.Content?.ToString();
                exam.SugerenciaMaterial = (CmbMaterial.SelectedItem as ComboBoxItem)?.Content?.ToString();

                PatientRepository.SaveClinicalExam(exam);

                await new ContentDialog { Title = "Éxito", Content = "Ficha clínica guardada correctamente.", CloseButtonText = "OK", XamlRoot = Content.XamlRoot }.ShowAsync();
                this.Close();
            }
            catch (Exception ex)
            {
                await new ContentDialog { Title = "Error", Content = $"Error al guardar: {ex.Message}", CloseButtonText = "OK", XamlRoot = Content.XamlRoot }.ShowAsync();
            }
        }

        // --- MÉTODOS AUXILIARES PARA GUARDAR/CARGAR LISTAS Y COMBOS ---

        private string GetCheckedString(params CheckBox[] boxes)
        {
            var list = new List<string>();
            foreach (var b in boxes)
            {
                if (b != null && b.IsChecked == true) list.Add(b.Content.ToString());
            }
            return string.Join(", ", list);
        }

        private void CheckFromString(string data, params CheckBox[] boxes)
        {
            if (string.IsNullOrEmpty(data)) return;
            foreach (var b in boxes)
            {
                if (b != null && data.Contains(b.Content.ToString())) b.IsChecked = true;
            }
        }

        private void SetComboValue(ComboBox cmb, string value)
        {
            if (string.IsNullOrEmpty(value) || cmb == null) return;
            foreach (ComboBoxItem item in cmb.Items)
            {
                if (item.Content.ToString() == value)
                {
                    cmb.SelectedItem = item;
                    break;
                }
            }
        }

        private void OnRefractionChanged(object sender, TextChangedEventArgs e)
        {
            decimal sphOD = Parse(TxtSphereOD.Text); decimal sphOI = Parse(TxtSphereOI.Text);
            decimal cylOD = Parse(TxtCylOD.Text); decimal cylOI = Parse(TxtCylOI.Text);
            decimal add = Parse(TxtAddOD.Text);

            if (ChkMiopia == null) return;

            ChkMiopia.IsChecked = (sphOD < 0 || sphOI < 0);
            ChkHipermetropia.IsChecked = (sphOD > 0 || sphOI > 0);
            ChkAstigmatismo.IsChecked = (cylOD != 0 || cylOI != 0);
            ChkPresbicia.IsChecked = (add > 0);
            ChkEmetropia.IsChecked = (sphOD == 0 && sphOI == 0 && cylOD == 0 && cylOI == 0 && add == 0);
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) => this.Close();
        private decimal Parse(string t) => decimal.TryParse(t, out decimal r) ? r : 0;
    }
}