using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using System;
using System.Collections.Generic;
using System.Linq;
using OpticaPro.Models;
using OpticaPro.Services;

namespace OpticaPro.Views
{
    public sealed partial class PrescriptionsPage : Page
    {
        // Usamos una lista de Pacientes directamente
        private List<Patient> _allPatients = new List<Patient>();

        public PrescriptionsPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            LoadPatients();
        }

        private void LoadPatients()
        {
            try
            {
                _allPatients.Clear();

                // 1. Obtener todos los pacientes directamente del repositorio
                // Asegúrate de que PatientRepository.GetAllPatients() devuelve List<Patient>
                var patients = PatientRepository.GetAllPatients();

                if (patients != null)
                {
                    // Ordenar alfabéticamente
                    _allPatients = patients.OrderBy(p => p.FullName).ToList();
                }

                UpdateList(_allPatients);
            }
            catch (Exception ex)
            {
                var dialog = new ContentDialog
                {
                    Title = "Error",
                    Content = $"No se pudieron cargar los pacientes: {ex.Message}",
                    CloseButtonText = "Cerrar",
                    XamlRoot = this.Content?.XamlRoot
                };
                _ = dialog.ShowAsync();
            }
        }

        private void UpdateList(List<Patient> list)
        {
            if (PrescriptionsList == null) return;

            PrescriptionsList.ItemsSource = list;

            if (EmptyState != null)
            {
                // Mostrar mensaje "No encontrado" si la lista está vacía
                EmptyState.Visibility = (list == null || list.Count == 0)
                                        ? Visibility.Visible
                                        : Visibility.Collapsed;
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            // Filtro simple por Nombre o DNI
            if (_allPatients == null) return;

            var query = sender.Text.ToLower().Trim();

            if (string.IsNullOrEmpty(query))
            {
                UpdateList(_allPatients);
            }
            else
            {
                var filtered = _allPatients.Where(p =>
                    (p.FullName != null && p.FullName.ToLower().Contains(query)) ||
                    (p.Dni != null && p.Dni.ToLower().Contains(query))
                ).ToList();

                UpdateList(filtered);
            }
        }

        // Método simplificado para abrir el historial/informe
        private void OpenReport_Click(object sender, RoutedEventArgs e)
        {
            // Obtenemos el paciente directamente del botón
            if (sender is Button btn && btn.Tag is Patient selectedPatient)
            {
                // Abrimos la ventana de historial con el paciente seleccionado
                var historyWindow = new HistoryPreviewWindow(selectedPatient);
                historyWindow.Activate();
            }
        }
    }
}