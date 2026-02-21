using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Diagnostics;

namespace OpticaPro.Views
{
    public sealed partial class PatientsPage : Page
    {
        public ObservableCollection<Patient> FilteredPatients { get; set; } = new();

        public PatientsPage()
        {
            this.InitializeComponent();
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            // Pequeña pausa para asegurar que la UI esté lista
            await Task.Yield();
            RefreshData();
        }

        private void OnEscapeInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            if (this.Frame.CanGoBack) { this.Frame.GoBack(); args.Handled = true; }
        }

        private void RefreshData()
        {
            try
            {
                FilteredPatients.Clear();
                var listaGlobal = PatientRepository.GetAllPatients();

                if (listaGlobal != null)
                {
                    foreach (var p in listaGlobal) FilteredPatients.Add(p);
                }
                UpdateUIState();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error cargando pacientes: {ex.Message}");
            }
        }

        private void UpdateUIState()
        {
            if (PatientsList != null) PatientsList.ItemsSource = FilteredPatients;

            if (EmptyState != null && PatientsList != null)
            {
                bool isEmpty = FilteredPatients.Count == 0;
                EmptyState.Visibility = isEmpty ? Visibility.Visible : Visibility.Collapsed;
                PatientsList.Visibility = isEmpty ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        // --- EVENTOS ---
        private void AddPatient_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(AddPatientPage));

        private void PatientsList_ItemClick(object sender, ItemClickEventArgs e)
        {
            if (e.ClickedItem is Patient p) Frame.Navigate(typeof(PatientHistoryPage), p);
        }

        private void ViewHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is Patient p) Frame.Navigate(typeof(PatientHistoryPage), p);
        }

        private void EditPatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is Patient p) Frame.Navigate(typeof(AddPatientPage), p);
        }

        private async void DeletePatient_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is Patient p)
            {
                ContentDialog d = new ContentDialog
                {
                    Title = "Eliminar Paciente",
                    Content = $"¿Eliminar a {p.FullName} permanentemente?",
                    PrimaryButtonText = "Eliminar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close,
                    XamlRoot = this.XamlRoot
                };

                if (await d.ShowAsync() == ContentDialogResult.Primary)
                {
                    PatientRepository.DeletePatient(p);
                    RefreshData();
                }
            }
        }

        private void NewAppointment_Click(object sender, RoutedEventArgs e)
        {
            Patient selected = null;
            if (sender is MenuFlyoutItem item && item.Tag is Patient p1) selected = p1;
            else if (sender is FrameworkElement elm && elm.DataContext is Patient p2) selected = p2;

            if (selected != null)
            {
                var window = new AddAppointmentWindow(selected);
                window.Activate();
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            try
            {
                var query = sender.Text.ToLower().Trim();
                var all = PatientRepository.GetAllPatients();
                FilteredPatients.Clear();

                if (string.IsNullOrEmpty(query))
                {
                    foreach (var p in all) FilteredPatients.Add(p);
                }
                else
                {
                    var res = all.Where(p =>
                        (p.FullName?.ToLower().Contains(query) ?? false) ||
                        (p.Dni?.Contains(query) ?? false) ||
                        (p.Email?.ToLower().Contains(query) ?? false));

                    foreach (var p in res) FilteredPatients.Add(p);
                }
                UpdateUIState();
            }
            catch { }
        }

        // =========================================================
        // FUNCIONES ESTÁTICAS (STATIC HELPERS)
        // Usamos estas en PatientsPage para evitar crashes, aunque 
        // el modelo tenga sus propias propiedades para DashboardPage.
        // =========================================================

        public static SolidColorBrush GetStatusColor(string status, string type)
        {
            string s = status ?? "";
            bool isDeuda = s.Equals("Deuda", StringComparison.OrdinalIgnoreCase);

            if (type == "bg")
                return isDeuda ? new SolidColorBrush(Color.FromArgb(255, 253, 236, 236)) : new SolidColorBrush(Color.FromArgb(255, 237, 247, 237));
            else
                return isDeuda ? new SolidColorBrush(Color.FromArgb(255, 198, 40, 40)) : new SolidColorBrush(Color.FromArgb(255, 30, 70, 32));
        }

        public static string FormatAge(int age) => age > 0 ? $"{age} años" : "N/A";

        public static string FormatInt(int val) => val.ToString();
    }
}