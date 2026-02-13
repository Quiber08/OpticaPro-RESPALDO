using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Models;
using OpticaPro.Services;
using System.Collections.ObjectModel;

namespace OpticaPro.Views
{
    public sealed partial class OrdersPage : Page
    {
        // Usamos ObservableCollection para que la interfaz reaccione a cambios automáticamente
        public ObservableCollection<OrderDisplayItem> OrdersList { get; set; } = new ObservableCollection<OrderDisplayItem>();

        // Copia de respaldo para el buscador
        private List<OrderDisplayItem> _allOrdersBackup = new List<OrderDisplayItem>();

        public OrdersPage()
        {
            this.InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            CargarDatos();
        }

        private void CargarDatos()
        {
            OrdersList.Clear();
            _allOrdersBackup.Clear();

            // 1. OBTENER TODOS LOS DATOS
            var allOrders = PatientRepository.GetAllOrders();
            var allPatients = PatientRepository.GetAllPatients();

            foreach (var order in allOrders)
            {
                // Buscamos el dueño del pedido.
                // Usamos ToString() en p.Id por seguridad (funciona si es int o string)
                var owner = allPatients.FirstOrDefault(p => p.Id.ToString() == order.PatientId);

                // Si no encontramos al dueño (tal vez fue borrado), creamos un dummy para que no falle
                if (owner == null)
                {
                    owner = new Patient
                    {
                        FullName = order.ClientName ?? "Cliente no encontrado",

                        // --- CORRECCIÓN DEL ERROR CS0029 ---
                        // El error indica que Patient.Id es de tipo STRING.
                        // Por tanto, asignamos directamente el string order.PatientId.
                        Id = order.PatientId ?? "0"
                    };
                }

                var displayItem = new OrderDisplayItem
                {
                    OrderData = order,
                    PatientOwner = owner
                };

                _allOrdersBackup.Add(displayItem);
            }

            // 2. Ordenar y mostrar
            // Ponemos primero los NO entregados, y luego por fecha descendente
            var sortedList = _allOrdersBackup
                .OrderBy(x => x.Status == "Entregado") // False (0) va antes que True (1)
                .ThenByDescending(x => x.OrderData.Id)
                .ToList();

            foreach (var item in sortedList)
            {
                OrdersList.Add(item);
            }

            // Asignar al ListView
            if (OrdersListView != null)
            {
                OrdersListView.ItemsSource = OrdersList;
            }

            ActualizarContadores();
        }

        private void ActualizarContadores()
        {
            if (_allOrdersBackup == null) return;

            int pendientes = _allOrdersBackup.Count(x => x.Status != "Entregado");
            int entregados = _allOrdersBackup.Count(x => x.Status == "Entregado");

            if (TxtPendingCount != null) TxtPendingCount.Text = pendientes.ToString();
            if (TxtDeliveredCount != null) TxtDeliveredCount.Text = entregados.ToString();
        }

        // --- BOTONES Y ACCIONES ---

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OrderDisplayItem item)
            {
                var window = new CreateOrderWindow(item);
                window.Activate();
                window.Closed += (s, args) => CargarDatos(); // Recargar al cerrar
            }
        }

        private async void BtnAbonar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OrderDisplayItem item)
            {
                if (item.Balance <= 0)
                {
                    await ShowAlert("Pagado", "Este pedido ya está cancelado.");
                    return;
                }

                TextBox input = new TextBox { Header = "Monto a abonar", PlaceholderText = "0.00" };
                // Aseguramos que solo escriban números
                input.BeforeTextChanging += (s, args) =>
                {
                    args.Cancel = args.NewText.Any(c => !char.IsDigit(c) && c != '.' && c != ',');
                };

                ContentDialog dialog = new ContentDialog
                {
                    Title = $"Abonar - {item.PatientName}",
                    Content = input,
                    PrimaryButtonText = "Aceptar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    if (decimal.TryParse(input.Text, out decimal monto) && monto > 0)
                    {
                        if (monto > item.Balance)
                        {
                            await ShowAlert("Error", "El abono no puede superar la deuda.");
                            return;
                        }

                        // Actualizar modelo
                        item.OrderData.Deposit += monto;
                        item.OrderData.Balance = item.OrderData.TotalAmount - item.OrderData.Deposit;

                        // Si pagó todo, sugerir cambiar estado
                        if (item.OrderData.Balance <= 0 && item.OrderData.Status != "Entregado")
                        {
                            item.OrderData.Status = "Pagado / En Taller";
                        }

                        // Guardar en BD
                        PatientRepository.SaveOrder(item.OrderData);

                        await ShowAlert("Éxito", "Abono registrado correctamente.");
                        CargarDatos();
                    }
                }
            }
        }

        private async void BtnEntregar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OrderDisplayItem item)
            {
                if (item.Balance > 0)
                {
                    ContentDialog warning = new ContentDialog
                    {
                        Title = "Saldo Pendiente",
                        Content = $"El cliente debe {item.FormattedBalance}. ¿Deseas marcar como entregado de todos modos?",
                        PrimaryButtonText = "Sí, Entregar",
                        CloseButtonText = "Cancelar",
                        XamlRoot = this.XamlRoot
                    };
                    if (await warning.ShowAsync() != ContentDialogResult.Primary) return;
                }

                item.OrderData.Status = "Entregado";
                item.OrderData.DeliveryDate = DateTime.Now.ToString("dd/MM/yyyy");

                PatientRepository.SaveOrder(item.OrderData);
                CargarDatos();
            }
        }

        // --- FILTROS Y BÚSQUEDA ---

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is string filtro)
            {
                OrdersList.Clear();
                IEnumerable<OrderDisplayItem> filtered;

                if (filtro == "All")
                    filtered = _allOrdersBackup;
                else if (filtro == "En Taller")
                    filtered = _allOrdersBackup.Where(x => x.Status != "Entregado");
                else
                    filtered = _allOrdersBackup.Where(x => x.Status == filtro);

                foreach (var item in filtered) OrdersList.Add(item);
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox box)
            {
                var query = box.Text.ToLower();
                OrdersList.Clear();

                var filtered = _allOrdersBackup.Where(x =>
                    (x.PatientName != null && x.PatientName.ToLower().Contains(query)) ||
                    (x.OrderData.Status != null && x.OrderData.Status.ToLower().Contains(query)) ||
                    (x.OrderData.FrameModel != null && x.OrderData.FrameModel.ToLower().Contains(query))
                );

                foreach (var item in filtered) OrdersList.Add(item);
            }
        }

        private async System.Threading.Tasks.Task ShowAlert(string title, string msg)
        {
            if (this.XamlRoot != null)
            {
                await new ContentDialog
                {
                    Title = title,
                    Content = msg,
                    CloseButtonText = "Entendido",
                    XamlRoot = this.XamlRoot
                }.ShowAsync();
            }
        }
    }
}