using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.Web.WebView2.Core;
using OpticaPro.Services;
using OpticaPro.Models;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Windows.UI;

namespace OpticaPro.Views
{
    // ==========================================
    // 1. MODELOS AUXILIARES
    // ==========================================
    public class OrderNotificationItem
    {
        public Patient Patient { get; set; }
        public Order Order { get; set; }

        public string PatientName => Patient?.FullName ?? "Desconocido";
        public string OrderDetails => $"{Order?.FrameModel} • {Order?.Status}";
        public string Status => Order?.Status;

        public SolidColorBrush StatusBg
        {
            get
            {
                var s = Status?.ToLower() ?? "";
                if (s.Contains("listo") || s.Contains("terminado") || s.Contains("entregar"))
                    return new SolidColorBrush(Color.FromArgb(255, 220, 255, 220));
                if (s.Contains("proceso") || s.Contains("taller"))
                    return new SolidColorBrush(Color.FromArgb(255, 220, 240, 255));
                return new SolidColorBrush(Color.FromArgb(255, 255, 240, 220));
            }
        }

        public SolidColorBrush StatusFg
        {
            get
            {
                var s = Status?.ToLower() ?? "";
                if (s.Contains("listo") || s.Contains("terminado") || s.Contains("entregar"))
                    return new SolidColorBrush(Color.FromArgb(255, 0, 100, 0));
                if (s.Contains("proceso") || s.Contains("taller"))
                    return new SolidColorBrush(Color.FromArgb(255, 0, 50, 150));
                return new SolidColorBrush(Color.FromArgb(255, 150, 80, 0));
            }
        }
    }

    public class PromoQueueItem : INotifyPropertyChanged
    {
        public Patient Patient { get; set; }
        public string Message { get; set; }
        public string DisplayName => Patient?.FullName;

        private bool _isSent;
        public bool IsSent
        {
            get => _isSent;
            set
            {
                _isSent = value;
                OnPropertyChanged(nameof(IsSent));
                OnPropertyChanged(nameof(IsPending));
            }
        }

        private string _statusText = "En espera";
        public string StatusText
        {
            get => _statusText;
            set
            {
                _statusText = value;
                OnPropertyChanged(nameof(StatusText));
            }
        }

        public bool IsPending => !IsSent;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged(string p) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(p));
    }

    // ==========================================
    // 2. CLASE PRINCIPAL DE LA VENTANA
    // ==========================================
    public sealed partial class MarketingWindow : Window
    {
        private ObservableCollection<OrderNotificationItem> _notificationItems = new();
        private ObservableCollection<PromoQueueItem> _promoQueue = new();
        private List<Patient> _allPatients;

        private bool _isProcessingQueue = false;
        private bool _isWindowClosed = false;
        private bool _isWebViewReady = false;
        private bool _hasLoadedData = false;

        public MarketingWindow()
        {
            this.InitializeComponent();

            // Gestionar cierre para modo Singleton
            this.Closed += (s, e) =>
            {
                _isWindowClosed = true;
                _isProcessingQueue = false;

                // Limpiamos la referencia en App para que se pueda volver a abrir si es necesario
                App.MarketingWindowInstance = null;

                try { WppBrowser?.Close(); } catch { }
            };

            ListReadyOrders.ItemsSource = _notificationItems;
            ListPromoQueue.ItemsSource = _promoQueue;

            this.Activated += MarketingWindow_Activated;
        }

        private async void MarketingWindow_Activated(object sender, WindowActivatedEventArgs args)
        {
            if (_hasLoadedData) return;
            _hasLoadedData = true;

            TxtPromoMsg.Text = "Hola {Nombre}, tenemos descuento ";

            // Intentamos iniciar el WebView apenas se active la ventana
            await InitializeWebViewSafe();
            LoadData();
        }

        private void BtnRefresh_Click(object sender, RoutedEventArgs e)
        {
            LoadData();
        }

        private void LoadData()
        {
            try
            {
                TxtStatus.Text = "Leyendo pacientes y pedidos...";

                var patients = PatientRepository.GetAllPatients();
                var orders = PatientRepository.GetAllOrders();

                if (patients == null)
                {
                    TxtStatus.Text = "Error: Base de datos devolvió nulo.";
                    _allPatients = new List<Patient>();
                }
                else
                {
                    foreach (var p in patients)
                    {
                        if (p.OrderHistory == null) p.OrderHistory = new List<Order>();

                        if (orders != null)
                        {
                            var susPedidos = orders.Where(o => o.PatientId == p.Id.ToString()).ToList();
                            p.OrderHistory.AddRange(susPedidos);
                        }
                    }

                    _allPatients = patients;
                    RefreshNotificationList();
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"Error BD: {ex.Message}";
                _allPatients = new List<Patient>();
            }
        }

        private void RefreshNotificationList()
        {
            if (_allPatients == null) return;
            _notificationItems.Clear();

            string filter = TxtSearchReady.Text?.ToLower() ?? "";
            int mode = CmbStatusFilter.SelectedIndex;
            int count = 0;

            foreach (var p in _allPatients)
            {
                if (p.OrderHistory == null) continue;

                foreach (var o in p.OrderHistory)
                {
                    bool match = false;
                    string s = o.Status?.ToLower() ?? "";

                    if (mode == 0) match = !s.Contains("entregado");
                    if (mode == 1) match = s.Contains("listo") || s.Contains("terminado");
                    if (mode == 2) match = s.Contains("proceso") || s.Contains("taller");

                    bool textMatch = string.IsNullOrEmpty(filter) ||
                                     (p.FullName != null && p.FullName.ToLower().Contains(filter)) ||
                                     (p.Dni != null && p.Dni.Contains(filter));

                    if (match && textMatch)
                    {
                        _notificationItems.Add(new OrderNotificationItem { Patient = p, Order = o });
                        count++;
                    }
                }
            }
            TxtStatus.Text = $"Datos cargados: {count} órdenes encontradas.";
        }

        private void FilterChanged(object sender, object e) => RefreshNotificationList();

        private async Task InitializeWebViewSafe()
        {
            try
            {
                if (WppBrowser != null && !_isWebViewReady)
                {
                    TxtStatus.Text = "Iniciando motor Web...";
                    await WppBrowser.EnsureCoreWebView2Async();

                    if (WppBrowser.Source == null || !WppBrowser.Source.ToString().Contains("web.whatsapp.com"))
                    {
                        WppBrowser.Source = new Uri("https://web.whatsapp.com");
                    }

                    _isWebViewReady = true;
                    TxtStatus.Text = "Sistema listo. Escanea QR si es necesario.";
                }
            }
            catch (Exception ex)
            {
                TxtStatus.Text = "Error WebView2: " + ex.Message;
            }
        }

        private void BtnToggleBrowser_Click(object sender, RoutedEventArgs e)
        {
            // Aquí es donde tú controlas manualmente si se ve o no.
            BrowserContainer.Height = (BrowserContainer.Height == 0) ? 600 : 0;
        }

        // =========================================================
        // 3. MÉTODO CORREGIDO: ENVÍO SIN ABRIR VENTANA
        // =========================================================
        public async Task EnviarMensajeDesdeOrders(string telefono, string mensaje)
        {
            // CORRECCIÓN: Eliminamos la línea que forzaba "BrowserContainer.Height = 600"
            // Ahora respeta si el usuario lo tiene oculto (Height=0).

            if (!_isWebViewReady)
            {
                TxtStatus.Text = "Iniciando motor en segundo plano...";
                await InitializeWebViewSafe();

                // Damos tiempo extra para cargar si estaba dormido
                await Task.Delay(4000);
            }

            // Enviamos el mensaje aunque el navegador no se vea
            await SendMessage(telefono, mensaje);
        }

        // ==========================================
        // 4. LÓGICA DE AUTOMATIZACIÓN (LINK INJECTION)
        // ==========================================

        private async Task SendMessage(string phone, string message)
        {
            if (_isWindowClosed) return;

            if (!_isWebViewReady || WppBrowser?.CoreWebView2 == null)
            {
                TxtStatus.Text = "⚠️ Navegador no listo. Espere...";
                return;
            }

            TxtStatus.Text = $"⏳ Conectando con {phone}...";

            try
            {
                string cleanPhone = new string(phone.Where(char.IsDigit).ToArray());
                if (cleanPhone.Length == 10 && cleanPhone.StartsWith("09"))
                    cleanPhone = "593" + cleanPhone.Substring(1);

                string encodedMsg = Uri.EscapeDataString(message);

                // Asegurar que estamos en WhatsApp Web
                if (!WppBrowser.Source.ToString().Contains("web.whatsapp.com"))
                {
                    WppBrowser.Source = new Uri("https://web.whatsapp.com");
                    await Task.Delay(8000);
                }

                // Inyección del enlace para abrir el chat
                string navigateScript = $@"
                    (function() {{
                        var link = document.createElement('a');
                        link.href = 'https://web.whatsapp.com/send?phone={cleanPhone}&text={encodedMsg}';
                        link.style.display = 'none';
                        document.body.appendChild(link);
                        link.click();
                        document.body.removeChild(link);
                        return 'NAVIGATED';
                    }})();";

                await WppBrowser.ExecuteScriptAsync(navigateScript);

                TxtStatus.Text = "⏳ Abriendo chat...";
                await Task.Delay(4500);

                // Verificar si el número es inválido
                string checkInvalidScript = @"
                    (function() {
                        var invalid = document.querySelector('div[data-animate-modal-popup=""true""]');
                        if (invalid && invalid.innerText.includes('inválido')) {
                            var btn = invalid.querySelector('div[role=""button""]');
                            if (btn) btn.click(); 
                            return 'INVALID';
                        }
                        return 'OK';
                    })();";

                string checkResult = await WppBrowser.ExecuteScriptAsync(checkInvalidScript);
                if (checkResult != null && checkResult.Contains("INVALID"))
                {
                    TxtStatus.Text = "⚠️ Número sin WhatsApp.";
                    if (_isProcessingQueue) ProcessNextInQueue(false);
                    return;
                }

                await Task.Delay(1000);
                await InjectClickScript();
            }
            catch (Exception ex)
            {
                TxtStatus.Text = $"❌ Error: {ex.Message}";
                if (_isProcessingQueue) ProcessNextInQueue(false);
            }
        }

        private async Task InjectClickScript()
        {
            if (_isWindowClosed) return;

            // Script para buscar y hacer clic en el botón de enviar
            string jsCode = @"
                (function() {
                    var btn = document.querySelector('span[data-icon=""send""]');
                    if (!btn) btn = document.querySelector('span[data-icon=""wds-ic-send-filled""]');
                    if (!btn) btn = document.querySelector('button[aria-label=""Send""]');
                    if (!btn) btn = document.querySelector('button[aria-label=""Enviar""]');

                    if (btn) {
                        var clickable = btn.closest('button') || btn.closest('div[role=""button""]');
                        if (clickable) {
                            clickable.click();
                            return 'EXITO';
                        }
                    }
                    return 'NO_ENCONTRADO';
                })();
            ";

            int intentos = 0;
            bool enviado = false;

            while (intentos < 5)
            {
                if (_isWindowClosed) return;
                try
                {
                    string result = await WppBrowser.ExecuteScriptAsync(jsCode);
                    if (result != null && result.Contains("EXITO"))
                    {
                        enviado = true;
                        break;
                    }
                }
                catch { }

                await Task.Delay(500);
                intentos++;
            }

            if (enviado)
            {
                TxtStatus.Text = "✅ Mensaje enviado.";
                if (_isProcessingQueue) ProcessNextInQueue(true);
            }
            else
            {
                TxtStatus.Text = "❌ No se pudo enviar (Botón no apareció).";
                if (_isProcessingQueue) ProcessNextInQueue(false);
            }
        }

        // ==========================================
        // 5. LÓGICA DE COLA MASIVA (CAMPAÑAS)
        // ==========================================

        private void BtnPrepare_Click(object sender, RoutedEventArgs e)
        {
            _promoQueue.Clear();
            string msg = TxtPromoMsg.Text;

            if (string.IsNullOrWhiteSpace(msg) || _allPatients == null)
            {
                TxtStatus.Text = "⚠️ Escribe un mensaje o carga pacientes primero.";
                return;
            }

            foreach (var p in _allPatients)
            {
                if (!string.IsNullOrEmpty(p.Phone) && p.Phone.Length >= 9)
                {
                    _promoQueue.Add(new PromoQueueItem
                    {
                        Patient = p,
                        Message = msg.Replace("{Nombre}", p.FullName),
                        IsSent = false
                    });
                }
            }
            TxtStatus.Text = $"Lista preparada: {_promoQueue.Count} destinatarios.";
        }

        private void BtnStartMass_Click(object sender, RoutedEventArgs e)
        {
            if (_promoQueue.Count == 0) return;

            if (_isProcessingQueue)
            {
                _isProcessingQueue = false;
                TxtStatus.Text = "⏸️ Campaña Pausada.";
                PromoProgress.IsIndeterminate = false;
                return;
            }

            _isProcessingQueue = true;
            PromoProgress.IsIndeterminate = true;
            TxtStatus.Text = "🚀 Iniciando campaña...";
            ProcessNextInQueue(true);
        }

        private PromoQueueItem _currentItem;

        private async void ProcessNextInQueue(bool prevSuccess)
        {
            if (!_isProcessingQueue || _isWindowClosed) return;

            if (_currentItem != null)
            {
                _currentItem.IsSent = prevSuccess;
                _currentItem.StatusText = prevSuccess ? "Enviado" : "Error";
            }

            var next = _promoQueue.FirstOrDefault(x => x.IsPending);
            _currentItem = next;

            if (next == null)
            {
                _isProcessingQueue = false;
                PromoProgress.IsIndeterminate = false;
                TxtStatus.Text = "🎉 Campaña finalizada.";
                return;
            }

            ListPromoQueue.ScrollIntoView(next);
            next.StatusText = "--> Procesando...";

            int delay = new Random().Next(5000, 8000);
            TxtStatus.Text = $"⏳ Esperando {delay / 1000}s...";
            await Task.Delay(delay);

            if (!_isProcessingQueue) return;

            await SendMessage(next.Patient.Phone, next.Message);
        }

        private async void BtnNotify_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is OrderNotificationItem item)
            {
                string msg = $"Hola {item.PatientName}, le saludamos de ÓpticaPro. Sus lentes están listos para retiro. ¡Le esperamos!";
                await SendMessage(item.Patient.Phone, msg);
            }
        }

        private async void BtnSendOne_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.DataContext is PromoQueueItem item)
            {
                await SendMessage(item.Patient.Phone, item.Message);
            }
        }
    }
}