using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI;
using System.Collections.ObjectModel;
using System;
using System.Threading.Tasks;
using System.Linq;
using OpticaPro.Services;
using OpticaPro.Models;
using Supabase.Realtime;
using Supabase.Postgrest;

namespace OpticaPro.Views
{
    public class ChatMessage
    {
        public string Message { get; set; }
        public string Time { get; set; }
        public bool IsMe { get; set; }

        public HorizontalAlignment HorizontalAlign => IsMe ? HorizontalAlignment.Right : HorizontalAlignment.Left;

        public SolidColorBrush BubbleColor => IsMe
            ? new SolidColorBrush(Windows.UI.Color.FromArgb(255, 0, 120, 215))
            : new SolidColorBrush(Windows.UI.Color.FromArgb(255, 45, 45, 45));

        public SolidColorBrush TextColor => new SolidColorBrush(Colors.White);
        public SolidColorBrush TimeColor => new SolidColorBrush(Colors.LightGray);
    }

    public sealed partial class SupportPage : Page
    {
        public static ObservableCollection<ChatMessage> GlobalMessages { get; set; } = new ObservableCollection<ChatMessage>();
        private string _myLicenseKey;
        private RealtimeChannel _licenseChannel;

        public SupportPage()
        {
            this.InitializeComponent();
            ChatList.ItemsSource = GlobalMessages;
            _myLicenseKey = SecurityService.GetLicenseKey();

            Loaded += SupportPage_Loaded;
            Unloaded += SupportPage_Unloaded;
        }

        private async void SupportPage_Loaded(object sender, RoutedEventArgs e)
        {
            // 1. REVISIÓN INMEDIATA Y LIMPIEZA DE NOTIFICACIÓN
            if (MainWindow.Current != null)
            {
                MainWindow.Current.UpdateSupportNotification(0);
            }

            await CheckLicenseDetails();

            if (string.IsNullOrEmpty(_myLicenseKey))
            {
                if (GlobalMessages.Count == 0)
                {
                    GlobalMessages.Add(new ChatMessage
                    {
                        Message = "Inicia sesión con una licencia válida para chatear.",
                        Time = DateTime.Now.ToShortTimeString(),
                        IsMe = false
                    });
                }
                return;
            }

            try
            {
                // 2. CARGAR HISTORIAL DE CHAT
                var history = await SupabaseService.Client.From<MessageModel>()
                    .Where(x => x.LicenseKey == _myLicenseKey)
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Ascending)
                    .Get();

                // --- NUEVO: MARCAR MENSAJES DE ADMIN COMO LEÍDOS ---
                var unreadMessages = history.Models.Where(m => m.IsAdmin && !m.IsRead).ToList();
                if (unreadMessages.Count > 0)
                {
                    foreach (var msg in unreadMessages)
                    {
                        // Actualizamos en silencio para que no salga notificación de nuevo
                        await SupabaseService.Client.From<MessageModel>()
                            .Where(x => x.Id == msg.Id)
                            .Set(x => x.IsRead, true)
                            .Update();
                    }
                }
                // ---------------------------------------------------

                GlobalMessages.Clear();
                foreach (var msg in history.Models)
                {
                    if (msg.Content == "CMD_CLEAR_CHAT") continue;

                    GlobalMessages.Add(new ChatMessage
                    {
                        Message = msg.Content,
                        IsMe = !msg.IsAdmin,
                        Time = msg.CreatedAt.ToLocalTime().ToString("HH:mm")
                    });
                }
                ScrollToBottom();

                // 3. SUSCRIPCIÓN AL CHAT EN VIVO
                var chatChannel = await SupabaseService.Client.From<MessageModel>()
                    .On(Supabase.Realtime.PostgresChanges.PostgresChangesOptions.ListenType.Inserts, (sender, change) =>
                    {
                        var newMsg = change.Model<MessageModel>();
                        if (newMsg != null && newMsg.LicenseKey == _myLicenseKey)
                        {
                            if (newMsg.Content == "CMD_CLEAR_CHAT")
                            {
                                this.DispatcherQueue.TryEnqueue(() => { GlobalMessages.Clear(); });
                                return;
                            }

                            if (newMsg.IsAdmin)
                            {
                                this.DispatcherQueue.TryEnqueue(() =>
                                {
                                    GlobalMessages.Add(new ChatMessage
                                    {
                                        Message = newMsg.Content,
                                        IsMe = false,
                                        Time = newMsg.CreatedAt.ToLocalTime().ToString("HH:mm")
                                    });
                                    ScrollToBottom();
                                });

                                // Si estamos en la página, marcamos como leído inmediatamente
                                Task.Run(async () =>
                                {
                                    await SupabaseService.Client.From<MessageModel>()
                                        .Where(x => x.Id == newMsg.Id)
                                        .Set(x => x.IsRead, true)
                                        .Update();
                                });
                            }
                        }
                    });

                await chatChannel.Subscribe();


                // 4. SUSCRIPCIÓN A LA LICENCIA
                _licenseChannel = await SupabaseService.Client.From<LicenseModel>()
                    .On(Supabase.Realtime.PostgresChanges.PostgresChangesOptions.ListenType.Updates, (sender, change) =>
                    {
                        var updatedLicense = change.Model<LicenseModel>();
                        if (updatedLicense != null && updatedLicense.LicenseKey == _myLicenseKey)
                        {
                            this.DispatcherQueue.TryEnqueue(() =>
                            {
                                UpdateLicenseUI(updatedLicense);
                            });
                        }
                    });

                await _licenseChannel.Subscribe();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error Realtime: {ex.Message}");
            }
        }

        private void SupportPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_licenseChannel != null)
            {
                _licenseChannel.Unsubscribe();
            }
        }

        private async Task CheckLicenseDetails()
        {
            if (string.IsNullOrEmpty(_myLicenseKey))
            {
                UpdateLicenseUI(null);
                return;
            }

            try
            {
                var result = await SupabaseService.Client.From<LicenseModel>()
                    .Where(x => x.LicenseKey == _myLicenseKey)
                    .Get();

                var license = result.Models.FirstOrDefault();

                this.DispatcherQueue.TryEnqueue(() => UpdateLicenseUI(license));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error fetching license: {ex.Message}");
            }
        }

        private void UpdateLicenseUI(LicenseModel license)
        {
            if (license == null)
            {
                TxtLicenseType.Text = "Licencia No Encontrada";
                TxtLicenseExpiry.Text = "---";
                IconLicenseStatus.Foreground = new SolidColorBrush(Colors.Red);
                return;
            }

            string typeCode = license.Type?.ToUpper() ?? "";
            string typeText;
            switch (typeCode)
            {
                case "MEN": typeText = "Licencia Mensual"; break;
                case "ANU": typeText = "Licencia Anual"; break;
                case "PER": typeText = "Licencia Permanente"; break;
                default: typeText = $"Licencia {typeCode}"; break;
            }

            string expiryText;
            bool isExpired = false;

            if (typeCode == "PER")
            {
                expiryText = "Vencimiento: De por vida";
            }
            else
            {
                var localExpiry = license.ExpiryDate.ToLocalTime();
                expiryText = $"Vence: {localExpiry:dd/MM/yyyy}";

                if (DateTime.Now > localExpiry)
                {
                    isExpired = true;
                    expiryText += " (VENCIDA)";
                }
            }

            TxtLicenseType.Text = typeText;
            TxtLicenseExpiry.Text = expiryText;

            if (!license.IsActive || isExpired)
            {
                IconLicenseStatus.Foreground = new SolidColorBrush(Colors.Red);
                if (!license.IsActive) TxtLicenseType.Text += " (Desactivada)";
            }
            else
            {
                IconLicenseStatus.Foreground = new SolidColorBrush(Colors.LimeGreen);
            }
        }

        private void SendMessage_Click(object sender, RoutedEventArgs e) => SendUserMessage();

        private void TxtMessage_KeyDown(object sender, KeyRoutedEventArgs e)
        {
            if (e.Key == Windows.System.VirtualKey.Enter) SendUserMessage();
        }

        private async void SendUserMessage()
        {
            if (string.IsNullOrWhiteSpace(TxtMessage.Text)) return;
            if (string.IsNullOrEmpty(_myLicenseKey)) return;

            string text = TxtMessage.Text;
            TxtMessage.Text = "";

            GlobalMessages.Add(new ChatMessage { Message = text, Time = DateTime.Now.ToShortTimeString(), IsMe = true });
            ScrollToBottom();

            await SupabaseService.SendMessage(_myLicenseKey, text);
        }

        private void ScrollToBottom()
        {
            if (GlobalMessages.Count > 0)
                ChatList.ScrollIntoView(GlobalMessages[GlobalMessages.Count - 1]);
        }

        private async void RefreshLicense_Click(object sender, RoutedEventArgs e)
        {
            TxtLicenseType.Text = "Verificando...";
            await CheckLicenseDetails();

            await Task.Delay(500);
            await new ContentDialog
            {
                Title = "Info",
                Content = "Estado sincronizado con el servidor.",
                CloseButtonText = "Ok",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }

        private async void EnterNewLicense_Click(object sender, RoutedEventArgs e)
        {
            await new ContentDialog
            {
                Title = "Nueva Licencia",
                Content = "Función disponible en próxima versión.",
                CloseButtonText = "Ok",
                XamlRoot = this.XamlRoot
            }.ShowAsync();
        }
    }
}