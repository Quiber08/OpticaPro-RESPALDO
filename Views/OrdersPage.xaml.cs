using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using Microsoft.UI.Xaml.Media;
using Windows.UI;
using OpticaPro.Models;
using OpticaPro.Services;

namespace OpticaPro.Views
{
    public sealed partial class OrdersPage : Page
    {
        public ObservableCollection<OrderDisplayItem> OrdersList { get; set; } = new ObservableCollection<OrderDisplayItem>();

        private List<OrderDisplayItem> _allOrdersBackup = new List<OrderDisplayItem>();
        private string _currentFilter = "All";

        public OrdersPage()
        {
            this.InitializeComponent();
            OrdersListView.ItemsSource = OrdersList;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            CargarDatos();
        }

        private void CargarDatos()
        {
            try
            {
                OrdersList.Clear();
                _allOrdersBackup.Clear();

                var allOrders = PatientRepository.GetAllOrders();
                var allPatients = PatientRepository.GetAllPatients();

                foreach (var order in allOrders)
                {
                    var owner = allPatients.FirstOrDefault(p => p.Id.ToString() == order.PatientId);

                    if (owner == null)
                    {
                        owner = new Patient
                        {
                            FullName = order.ClientName ?? "Desconocido",
                            Id = order.PatientId ?? "0",
                            Phone = ""
                        };
                    }

                    var displayItem = new OrderDisplayItem
                    {
                        OrderData = order,
                        PatientOwner = owner
                    };

                    _allOrdersBackup.Add(displayItem);
                }

                ApplyFiltersAndSearch();
                ActualizarContadores();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error: {ex.Message}");
            }
        }

        private void ActualizarContadores()
        {
            if (_allOrdersBackup == null) return;

            int pendientes = _allOrdersBackup.Count(x => x.Status != "Entregado");
            int entregados = _allOrdersBackup.Count(x => x.Status == "Entregado");

            if (TxtPendingCount != null) TxtPendingCount.Text = pendientes.ToString();
            if (TxtDeliveredCount != null) TxtDeliveredCount.Text = entregados.ToString();
        }

        private void ApplyFiltersAndSearch()
        {
            if (_allOrdersBackup == null) return;

            string query = SearchBox?.Text?.ToLower() ?? "";
            var temp = _allOrdersBackup.AsEnumerable();

            if (_currentFilter == "En Taller")
            {
                temp = temp.Where(x => x.Status != "Entregado");
            }
            else if (_currentFilter == "Entregado")
            {
                temp = temp.Where(x => x.Status == "Entregado");
            }

            if (!string.IsNullOrWhiteSpace(query))
            {
                temp = temp.Where(x =>
                    (x.PatientName != null && x.PatientName.ToLower().Contains(query)) ||
                    (x.OrderData.FrameModel != null && x.OrderData.FrameModel.ToLower().Contains(query)) ||
                    (x.Status != null && x.Status.ToLower().Contains(query))
                );
            }

            var finalResult = temp
                .OrderBy(x => x.Status == "Entregado")
                .ThenByDescending(x => x.OrderData.Id)
                .ToList();

            OrdersList.Clear();
            foreach (var item in finalResult)
            {
                OrdersList.Add(item);
            }

            if (EmptyStatePanel != null)
            {
                EmptyStatePanel.Visibility = OrdersList.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        private void Filter_Click(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton btn && btn.Tag is string filtro)
            {
                _currentFilter = filtro;
                ApplyFiltersAndSearch();
            }
        }

        private void SearchBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFiltersAndSearch();
        }

        private void ClearSearch_Click(object sender, RoutedEventArgs e)
        {
            if (SearchBox != null) SearchBox.Text = "";
        }

        // =========================================================
        // ACCIONES CONEXIÓN MARKETING (MODO FANTASMA)
        // =========================================================

        private async void BtnWhatsApp_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OrderDisplayItem item)
            {
                string telefono = item.PatientOwner.Phone;

                if (string.IsNullOrWhiteSpace(telefono))
                {
                    await ShowAlert("Sin teléfono", "El cliente no tiene un número registrado.");
                    return;
                }

                StackPanel optionsPanel = new StackPanel { Spacing = 10 };

                RadioButton rbReady = new RadioButton { Content = "✅ Aviso: Su pedido está LISTO", IsChecked = true, FontSize = 14 };
                RadioButton rbBalance = new RadioButton { Content = $"💰 Cobro: Recordar saldo de {item.FormattedBalance}", FontSize = 14 };
                RadioButton rbProcess = new RadioButton { Content = "🔨 Estado: Sigue en taller", FontSize = 14 };
                RadioButton rbThanks = new RadioButton { Content = "👋 Cortesía: Gracias por su compra", FontSize = 14 };

                optionsPanel.Children.Add(rbReady);
                if (item.Balance > 0) optionsPanel.Children.Add(rbBalance);
                optionsPanel.Children.Add(rbProcess);
                optionsPanel.Children.Add(rbThanks);

                ContentDialog dialog = new ContentDialog
                {
                    Title = $"Mensaje para {item.PatientName}",
                    Content = optionsPanel,
                    PrimaryButtonText = "Enviar Auto-WhatsApp",
                    CloseButtonText = "Cancelar",
                    DefaultButton = ContentDialogButton.Primary,
                    XamlRoot = this.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    string saludo = $"Hola {item.PatientName}, le saludamos de ÓpticaPro.";
                    string detalle = !string.IsNullOrEmpty(item.OrderData.FrameModel) ? $"Ref: {item.OrderData.FrameModel}." : "";
                    string cuerpo = "";

                    if (rbReady.IsChecked == true) cuerpo = "✨ ¡Buenas noticias! Su pedido ya está LISTO para retirar.";
                    else if (rbBalance.IsChecked == true) cuerpo = $"Le recordamos que su pedido tiene un saldo pendiente de {item.FormattedBalance}.";
                    else if (rbProcess.IsChecked == true) cuerpo = "Su trabajo sigue en proceso en el taller. Le avisaremos al terminar.";
                    else if (rbThanks.IsChecked == true) cuerpo = "Esperamos que disfrute sus lentes. Estamos a las órdenes.";

                    string mensajeFinal = $"{saludo} {detalle}\n\n{cuerpo}";

                    // --- MODO FANTASMA: Reutilizar instancia ---
                    try
                    {
                        // 1. Verificamos si existe la instancia global
                        if (App.MarketingWindowInstance == null)
                        {
                            App.MarketingWindowInstance = new MarketingWindow();
                            App.MarketingWindowInstance.Activate(); // Abrir si no existe
                        }

                        // 2. Usamos la instancia global SIN activarla obligatoriamente (para que no salte al frente)
                        // Si quieres que no robe el foco, no llames a Activate() aquí.
                        // Solo llamamos al método de envío.
                        await App.MarketingWindowInstance.EnviarMensajeDesdeOrders(telefono, mensajeFinal);
                    }
                    catch (Exception ex)
                    {
                        await ShowAlert("Error", "No se pudo conectar con el módulo de Marketing: " + ex.Message);
                    }
                }
            }
        }

        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OrderDisplayItem item)
            {
                var window = new CreateOrderWindow(item);
                window.Activate();
                window.Closed += (s, args) => CargarDatos();
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

                TextBox input = new TextBox
                {
                    Header = "Monto a abonar",
                    PlaceholderText = "0.00",
                    InputScope = new Microsoft.UI.Xaml.Input.InputScope { Names = { new Microsoft.UI.Xaml.Input.InputScopeName(Microsoft.UI.Xaml.Input.InputScopeNameValue.Number) } }
                };

                input.BeforeTextChanging += (s, args) => args.Cancel = args.NewText.Any(c => !char.IsDigit(c) && c != '.' && c != ',');

                ContentDialog dialog = new ContentDialog
                {
                    Title = "Registrar Abono",
                    Content = input,
                    PrimaryButtonText = "Aceptar",
                    CloseButtonText = "Cancelar",
                    XamlRoot = this.XamlRoot
                };

                if (await dialog.ShowAsync() == ContentDialogResult.Primary)
                {
                    if (decimal.TryParse(input.Text, out decimal monto) && monto > 0)
                    {
                        if (monto > item.Balance) { await ShowAlert("Error", "El abono supera la deuda."); return; }

                        item.OrderData.Deposit += monto;
                        item.OrderData.Balance = item.OrderData.TotalAmount - item.OrderData.Deposit;
                        if (item.OrderData.Balance <= 0 && item.OrderData.Status != "Entregado") item.OrderData.Status = "Pagado / En Taller";

                        PatientRepository.SaveOrder(item.OrderData);
                        await ShowAlert("Éxito", "Abono registrado.");
                        CargarDatos();
                    }
                }
            }
        }

        private async void BtnEntregar_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is OrderDisplayItem item)
            {
                if (item.Status == "Entregado") return;

                if (item.Balance > 0)
                {
                    ContentDialog warning = new ContentDialog
                    {
                        Title = "Saldo Pendiente",
                        Content = $"El cliente debe {item.FormattedBalance}. ¿Entregar de todos modos?",
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

        private async System.Threading.Tasks.Task ShowAlert(string title, string msg)
        {
            if (this.XamlRoot != null)
            {
                await new ContentDialog { Title = title, Content = msg, CloseButtonText = "Entendido", XamlRoot = this.XamlRoot }.ShowAsync();
            }
        }
    }
}