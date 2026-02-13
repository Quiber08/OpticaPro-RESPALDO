using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Models;
using OpticaPro.Services;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace OpticaPro.Views
{
    public sealed partial class PatientHistoryPage : Page, INotifyPropertyChanged
    {
        private Patient _currentPatient;
        private List<Appointment> _appointmentHistory;

        public event PropertyChangedEventHandler PropertyChanged;

        public Patient CurrentPatient
        {
            get => _currentPatient;
            set { _currentPatient = value; OnPropertyChanged(); }
        }

        public List<Appointment> AppointmentHistory
        {
            get => _appointmentHistory;
            set { _appointmentHistory = value; OnPropertyChanged(); }
        }

        public PatientHistoryPage()
        {
            // SI ESTA LINEA DA ERROR: Sigue las instrucciones de abajo "Pasos Obligatorios"
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);

            if (e.Parameter is Patient patient)
            {
                CurrentPatient = patient;

                // 1. Cargar Citas
                RefreshAppointmentsList();

                // 2. Cargar Exámenes
                RefreshExamsList();

                // 3. Cargar Órdenes
                RefreshOrdersList();
            }
        }

        private void RefreshAppointmentsList()
        {
            try
            {
                if (CurrentPatient != null)
                {
                    AppointmentHistory = AppointmentRepository.GetByPatient(CurrentPatient.Id);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error citas: {ex.Message}");
            }
        }

        private void RefreshExamsList()
        {
            if (CurrentPatient != null)
            {
                var examsFromDb = PatientRepository.GetExamsByPatientId(CurrentPatient.Id);
                CurrentPatient.ExamHistory = examsFromDb;
                OnPropertyChanged(nameof(CurrentPatient));
            }
        }

        private void RefreshOrdersList()
        {
            if (CurrentPatient.OrderHistory != null)
            {
                var nuevaLista = new List<Order>(CurrentPatient.OrderHistory);
                CurrentPatient.OrderHistory = nuevaLista;
                OnPropertyChanged(nameof(CurrentPatient));
            }
        }

        private void NewAppointment_Click(object sender, RoutedEventArgs e)
        {
            var win = new AddAppointmentWindow(CurrentPatient);
            win.Activate();
            win.Closed += (s, args) => this.DispatcherQueue.TryEnqueue(RefreshAppointmentsList);
        }

        private void NewExam_Click(object sender, RoutedEventArgs e)
        {
            var win = new ClinicalExamPage(CurrentPatient);
            win.Activate();
            win.Closed += (s, args) => this.DispatcherQueue.TryEnqueue(RefreshExamsList);
        }

        private void EditExam_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is ClinicalExam exam)
            {
                var win = new ClinicalExamPage(CurrentPatient, exam);
                win.Activate();
                win.Closed += (s, args) => this.DispatcherQueue.TryEnqueue(RefreshExamsList);
            }
        }

        private void NewOrder_Click(object sender, RoutedEventArgs e)
        {
            var win = new CreateOrderWindow(CurrentPatient);
            win.Activate();
            win.Closed += (s, args) => this.DispatcherQueue.TryEnqueue(RefreshOrdersList);
        }

        private async void ViewPayments_Click(object sender, RoutedEventArgs e)
        {
            decimal deuda = 0;
            if (CurrentPatient.OrderHistory != null)
            {
                foreach (var o in CurrentPatient.OrderHistory)
                    if (o.Balance > 0) deuda += o.Balance;
            }

            ContentDialog d = new ContentDialog
            {
                Title = "Estado de Cuenta",
                Content = $"Total a Pagar: {deuda:C2}",
                CloseButtonText = "OK",
                XamlRoot = this.XamlRoot
            };
            await d.ShowAsync();
        }

        private void Edit_Click(object sender, RoutedEventArgs e) => Frame.Navigate(typeof(AddPatientPage), CurrentPatient);

        private void Back_Click(object sender, RoutedEventArgs e) { if (Frame.CanGoBack) Frame.GoBack(); }

        private void OnEscapeInvoked(KeyboardAccelerator sender, KeyboardAcceleratorInvokedEventArgs args)
        {
            Back_Click(null, null);
            args.Handled = true;
        }

        public Visibility ShowIfEmpty(int count) => count == 0 ? Visibility.Visible : Visibility.Collapsed;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}