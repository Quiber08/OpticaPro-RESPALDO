using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace OpticaPro.Views
{
    public sealed partial class AppointmentsPage : Page
    {
        public ObservableCollection<Appointment> AllAppointments { get; set; } = new();
        public ObservableCollection<Appointment> PendingAppointments { get; set; } = new();
        public ObservableCollection<Appointment> CompletedAppointments { get; set; } = new();
        public ObservableCollection<Appointment> CanceledAppointments { get; set; } = new();

        private List<Appointment> _masterList = new();

        public AppointmentsPage()
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
                // Ahora AppointmentRepository.GetAllAppointments() sí existe
                var data = AppointmentRepository.GetAllAppointments();

                _masterList = data
                    .OrderByDescending(a => ParseDate(a.Date))
                    .ThenBy(a => a.Time)
                    .ToList();

                ApplyFilters();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error cargando citas: {ex.Message}");
            }
        }

        private void ApplyFilters()
        {
            string query = SearchBox.Text?.ToLower() ?? "";
            DateTimeOffset? filterDate = FilterDate.Date;

            var filtered = _masterList.Where(a =>
            {
                bool matchesText = string.IsNullOrEmpty(query) ||
                                   (a.PatientName?.ToLower().Contains(query) ?? false) ||
                                   (a.Reason?.ToLower().Contains(query) ?? false);

                bool matchesDate = true;
                if (filterDate.HasValue)
                {
                    string targetDate = filterDate.Value.ToString("dd/MM/yyyy");
                    matchesDate = a.Date == targetDate;
                }

                return matchesText && matchesDate;
            }).ToList();

            AllAppointments.Clear();
            PendingAppointments.Clear();
            CompletedAppointments.Clear();
            CanceledAppointments.Clear();

            foreach (var appt in filtered)
            {
                SetVisualProperties(appt); // Pinta los colores

                AllAppointments.Add(appt);

                string status = appt.Status?.ToLower() ?? "";
                if (status.Contains("pendiente"))
                {
                    PendingAppointments.Add(appt);
                }
                else if (status.Contains("completada") || status.Contains("finalizada"))
                {
                    CompletedAppointments.Add(appt);
                }
                else if (status.Contains("cancelada") || status.Contains("anulada"))
                {
                    CanceledAppointments.Add(appt);
                }
            }

            if (ListAll != null) ListAll.ItemsSource = AllAppointments;
            if (ListPending != null) ListPending.ItemsSource = PendingAppointments;
            if (ListCompleted != null) ListCompleted.ItemsSource = CompletedAppointments;
            if (ListCanceled != null) ListCanceled.ItemsSource = CanceledAppointments;

            UpdateUIStats();
            UpdateEmptyStates();
        }

        private void UpdateUIStats()
        {
            TxtStatsTotal.Text = AllAppointments.Count.ToString();
            TxtStatsPending.Text = PendingAppointments.Count.ToString();

            string todayStr = DateTime.Now.ToString("dd/MM/yyyy");
            int todayCount = AllAppointments.Count(a => a.Date == todayStr);
            TxtStatsToday.Text = todayCount.ToString();
        }

        private void UpdateEmptyStates()
        {
            if (EmptyAll != null) EmptyAll.Visibility = AllAppointments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (EmptyPending != null) EmptyPending.Visibility = PendingAppointments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (EmptyCompleted != null) EmptyCompleted.Visibility = CompletedAppointments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            if (EmptyCanceled != null) EmptyCanceled.Visibility = CanceledAppointments.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
        }

        private void SetVisualProperties(Appointment appt)
        {
            string status = appt.Status?.ToLower() ?? "";

            if (status.Contains("pendiente"))
            {
                appt.StatusColorBrush = new SolidColorBrush(Microsoft.UI.Colors.Orange);
                appt.StatusBackgroundBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(30, 255, 165, 0));
                appt.StatusTextBrush = new SolidColorBrush(Microsoft.UI.Colors.DarkOrange);
            }
            else if (status.Contains("completada"))
            {
                appt.StatusColorBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
                appt.StatusBackgroundBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(30, 0, 128, 0));
                appt.StatusTextBrush = new SolidColorBrush(Microsoft.UI.Colors.Green);
            }
            else if (status.Contains("cancelada"))
            {
                appt.StatusColorBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
                appt.StatusBackgroundBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(30, 255, 0, 0));
                appt.StatusTextBrush = new SolidColorBrush(Microsoft.UI.Colors.Red);
            }
            else
            {
                appt.StatusColorBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray);
                appt.StatusBackgroundBrush = new SolidColorBrush(Microsoft.UI.ColorHelper.FromArgb(30, 128, 128, 128));
                appt.StatusTextBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray);
            }
        }

        private void SearchBox_TextChanged(AutoSuggestBox sender, AutoSuggestBoxTextChangedEventArgs args)
        {
            ApplyFilters();
        }

        private void FilterDate_DateChanged(CalendarDatePicker sender, CalendarDatePickerDateChangedEventArgs args)
        {
            ApplyFilters();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            SearchBox.Text = "";
            FilterDate.Date = null;
            ApplyFilters();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void MainPivot_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
        }

        private void BtnNew_Click(object sender, RoutedEventArgs e)
        {
            var window = new AddAppointmentWindow();
            window.Activate();
            window.Closed += (s, args) => this.DispatcherQueue.TryEnqueue(LoadData);
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is Appointment appt)
            {
                var window = new AddAppointmentWindow(appt);
                window.Activate();
                window.Closed += (s, args) => this.DispatcherQueue.TryEnqueue(LoadData);
            }
        }

        private async void BtnDelete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is Appointment appt)
            {
                ContentDialog dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "Eliminar Cita",
                    Content = $"\u00BFConfirma que desea eliminar la cita de {appt.PatientName} permanentemente?",
                    PrimaryButtonText = "Eliminar",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Close
                };

                var result = await dialog.ShowAsync();
                if (result == ContentDialogResult.Primary)
                {
                    // Ahora AppointmentRepository.DeleteAppointment sí existe
                    AppointmentRepository.DeleteAppointment(appt);
                    LoadData();
                }
            }
        }

        private void BtnComplete_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is Appointment appt)
            {
                UpdateStatus(appt, "Completada");
            }
        }

        private void BtnCancelStatus_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuFlyoutItem item && item.Tag is Appointment appt)
            {
                ContentDialog dialog = new ContentDialog
                {
                    XamlRoot = this.XamlRoot,
                    Title = "Cancelar Cita",
                    Content = "La cita quedar\u00E1 registrada como cancelada. \u00BFContinuar?",
                    PrimaryButtonText = "Sí, Cancelar",
                    CloseButtonText = "Volver",
                    DefaultButton = ContentDialogButton.Close
                };

                _ = ShowCancelDialog(dialog, appt);
            }
        }

        private async System.Threading.Tasks.Task ShowCancelDialog(ContentDialog dialog, Appointment appt)
        {
            var result = await dialog.ShowAsync();
            if (result == ContentDialogResult.Primary)
            {
                UpdateStatus(appt, "Cancelada");
            }
        }

        private void UpdateStatus(Appointment appt, string newStatus)
        {
            appt.Status = newStatus;
            AppointmentRepository.UpdateAppointment(appt);
            LoadData();
        }

        private DateTime ParseDate(string dateStr)
        {
            if (DateTime.TryParseExact(dateStr, "dd/MM/yyyy",
                System.Globalization.CultureInfo.InvariantCulture,
                System.Globalization.DateTimeStyles.None, out DateTime result))
            {
                return result;
            }
            return DateTime.MinValue;
        }
    }
}