using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using OpticaPro.Views;
using OpticaPro.Services;
using OpticaPro.Models;

namespace OpticaPro
{
    public sealed partial class MainWindow : Window
    {
        public static MainWindow Current { get; private set; }

        private Views.OptotypeWindow _optotypeWindow;
        private Views.MarketingWindow _marketingWindow;
        private DispatcherTimer _safetyTimer;
        private bool _isRenewalDialogOpen = false;

        // =========================================================
        // CODIGO DE BAJO NIVEL (USER32.DLL)
        // =========================================================
        private const int GWLP_HWNDPARENT = -8;

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        private static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8)
                return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            else
                return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        public MainWindow()
        {
            this.InitializeComponent();

            // --- ASEGURAMIENTO DE REFERENCIA ---
            Current = this;
            App.m_window = this; // Vital para FileSavePicker
            // -----------------------------------

            this.Title = "OpticaPro";
            this.SystemBackdrop = null;
            this.ExtendsContentIntoTitleBar = true;

            NavView.BackRequested += NavView_BackRequested;
            ContentFrame.Navigated += ContentFrame_Navigated;

            var dashboardItem = NavView.MenuItems
                .OfType<NavigationViewItem>()
                .FirstOrDefault(i => i.Tag?.ToString() == "Dashboard");

            if (dashboardItem != null)
            {
                NavView.SelectedItem = dashboardItem;
            }

            ContentFrame.Navigate(typeof(DashboardPage));

            UpdateUserProfileUI();
            ApplyPermissions();

            SecurityService.OnProfileChanged += () =>
            {
                this.DispatcherQueue.TryEnqueue(() => UpdateUserProfileUI());
            };

            SetTitleBarTheme(ElementTheme.Default);
            InitializeSecurityWatchdog();
            InitializeUpdateSystem();
        }

        private void OpenChildWindow(Window childWindow)
        {
            if (childWindow == null) return;
            IntPtr mainHandle = WinRT.Interop.WindowNative.GetWindowHandle(this);
            IntPtr childHandle = WinRT.Interop.WindowNative.GetWindowHandle(childWindow);
            SetWindowLongPtr(childHandle, GWLP_HWNDPARENT, mainHandle);
            childWindow.Activate();
        }

        private void NavView_ItemInvoked(NavigationView sender, NavigationViewItemInvokedEventArgs args)
        {
            if (args.InvokedItemContainer?.Tag?.ToString() == "Logout")
            {
                var loginWindow = new LoginWindow();
                // Al volver al login, actualizamos la referencia también
                App.m_window = loginWindow;
                loginWindow.Activate();
                this.Close();
                return;
            }

            if (args.InvokedItemContainer != null && args.InvokedItemContainer.Tag != null)
            {
                string tag = args.InvokedItemContainer.Tag.ToString();

                switch (tag)
                {
                    case "Dashboard": ContentFrame.Navigate(typeof(DashboardPage)); break;
                    case "Patients": ContentFrame.Navigate(typeof(PatientsPage)); break;
                    case "AppointmentsPage": ContentFrame.Navigate(typeof(Views.AppointmentsPage)); break;
                    case "Orders": ContentFrame.Navigate(typeof(OrdersPage)); break;
                    case "Inventory": ContentFrame.Navigate(typeof(InventoryPage)); break;
                    case "Prescriptions": ContentFrame.Navigate(typeof(PrescriptionsPage)); break;
                    case "Marketing":
                        if (_marketingWindow == null)
                        {
                            _marketingWindow = new Views.MarketingWindow();
                            _marketingWindow.Closed += (s, ev) => _marketingWindow = null;
                            OpenChildWindow(_marketingWindow);
                        }
                        else _marketingWindow.Activate();
                        break;
                    case "Certificates": ContentFrame.Navigate(typeof(CertificatesPage)); break;
                    case "Financials": ContentFrame.Navigate(typeof(FinancialReportPage)); break;
                    case "Support": ContentFrame.Navigate(typeof(SupportPage)); break;
                    case "Users": ContentFrame.Navigate(typeof(UsersPage)); break;
                    case "Optotype":
                        if (_optotypeWindow == null)
                        {
                            _optotypeWindow = new Views.OptotypeWindow();
                            _optotypeWindow.Closed += (s, ev) => _optotypeWindow = null;
                            OpenChildWindow(_optotypeWindow);
                        }
                        else _optotypeWindow.Activate();
                        NavView.SelectedItem = null;
                        break;
                    case "Settings":
                        if (SecurityService.CurrentUserRole == "Admin")
                            ContentFrame.Navigate(typeof(SettingsPage));
                        break;
                }
            }
        }

        private void Profile_Tapped(object sender, TappedRoutedEventArgs e)
        {
            if (SecurityService.CurrentUserRole == "Admin")
            {
                ContentFrame.Navigate(typeof(SettingsPage));
                NavView.SelectedItem = NavView.SettingsItem;
            }
        }

        public void UpdateUserProfileUI()
        {
            string name = SecurityService.CurrentUserName;
            string imgPath = SecurityService.CurrentProfileImage;

            if (SidebarProfileName != null)
                SidebarProfileName.Text = string.IsNullOrEmpty(name) ? "Usuario" : name;

            if (SidebarProfilePic != null)
            {
                SidebarProfilePic.DisplayName = name;
                if (!string.IsNullOrEmpty(imgPath) && System.IO.File.Exists(imgPath))
                {
                    try { SidebarProfilePic.ProfilePicture = new BitmapImage(new Uri(imgPath)); }
                    catch { SidebarProfilePic.ProfilePicture = null; }
                }
                else SidebarProfilePic.ProfilePicture = null;

                if (!string.IsNullOrEmpty(name))
                {
                    var parts = name.Split(' ');
                    string initials = parts[0].Substring(0, 1).ToUpper();
                    if (parts.Length > 1) initials += parts[1].Substring(0, 1).ToUpper();
                    SidebarProfilePic.Initials = initials;
                }
            }
        }

        public void UpdateUserName(string newName) => UpdateUserProfileUI();

        private void ApplyPermissions()
        {
            string role = SecurityService.CurrentUserRole;
            foreach (var item in NavView.MenuItems.OfType<NavigationViewItem>()) item.Visibility = Visibility.Visible;
            foreach (var item in NavView.FooterMenuItems.OfType<NavigationViewItem>()) item.Visibility = Visibility.Visible;

            if (role == "Vendedor")
            {
                var settings = FindNavItem("Settings"); if (settings != null) settings.Visibility = Visibility.Collapsed;
                var users = FindNavItem("Users"); if (users != null) users.Visibility = Visibility.Collapsed;
            }
            else if (role == "Optometrista")
            {
                var orders = FindNavItem("Orders"); if (orders != null) orders.Visibility = Visibility.Collapsed;
                var inventory = FindNavItem("Inventory"); if (inventory != null) inventory.Visibility = Visibility.Collapsed;
                var financials = FindNavItem("Financials"); if (financials != null) financials.Visibility = Visibility.Collapsed;
                var settings = FindNavItem("Settings"); if (settings != null) settings.Visibility = Visibility.Collapsed;
            }
        }

        private NavigationViewItem FindNavItem(string tag)
        {
            var mainItem = NavView.MenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == tag);
            if (mainItem != null) return mainItem;
            return NavView.FooterMenuItems.OfType<NavigationViewItem>().FirstOrDefault(i => i.Tag?.ToString() == tag);
        }

        public void UpdateOrdersNotification(int count)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (BadgeOrders == null) return;
                if (count > 0)
                {
                    BadgeOrders.Value = count;
                    BadgeOrders.Visibility = Visibility.Visible;
                }
                else
                {
                    BadgeOrders.Visibility = Visibility.Collapsed;
                }
            });
        }

        public void UpdateSupportNotification(int count)
        {
            this.DispatcherQueue.TryEnqueue(() =>
            {
                if (BadgeSupport == null) return;
                if (count > 0)
                {
                    BadgeSupport.Value = count;
                    BadgeSupport.Visibility = Visibility.Visible;
                }
                else
                {
                    BadgeSupport.Visibility = Visibility.Collapsed;
                }
            });
        }

        private async void InitializeSecurityWatchdog()
        {
            string key = SecurityService.GetLicenseKey();
            if (string.IsNullOrEmpty(key) || key == "OFFLINE-MODE") return;

            bool esValida = await SecurityService.IsLicenseValidOnline();
            if (!esValida)
            {
                await ShowRenewalDialog("Tu licencia ha caducado.");
            }

            await CheckUnreadMessages();

            await SupabaseService.StartLicenseWatchdog(key, () =>
            {
                this.DispatcherQueue.TryEnqueue(async () =>
                {
                    if (_safetyTimer != null) _safetyTimer.Stop();
                    await ShowRenewalDialog("Licencia revocada o modificada en tiempo real.");
                });
            });

            if (_safetyTimer == null)
            {
                _safetyTimer = new DispatcherTimer();
                _safetyTimer.Interval = TimeSpan.FromMinutes(1);
                _safetyTimer.Tick += async (s, e) =>
                {
                    if (_isRenewalDialogOpen) return;

                    bool stillValid = await SecurityService.IsLicenseValidOnline();
                    if (!stillValid)
                    {
                        _safetyTimer.Stop();
                        await ShowRenewalDialog("La licencia expiró (verificación automática).");
                    }

                    await CheckUnreadMessages();
                };
            }
            _safetyTimer.Start();
        }

        private async Task CheckUnreadMessages()
        {
            try
            {
                string key = SecurityService.GetLicenseKey();
                if (string.IsNullOrEmpty(key) || key == "OFFLINE-MODE") return;

                var result = await SupabaseService.Client.From<MessageModel>()
                    .Where(x => x.LicenseKey == key && x.IsAdmin == true && x.IsRead == false)
                    .Get();

                int count = result.Models.Count;
                UpdateSupportNotification(count);
            }
            catch { }
        }

        private async Task ShowRenewalDialog(string reason)
        {
            if (_isRenewalDialogOpen) return;
            _isRenewalDialogOpen = true;

            var currentRoot = this.Content?.XamlRoot;
            if (currentRoot == null) return;

            StackPanel panel = new StackPanel { Spacing = 10, Width = 400 };

            TextBlock msgBlock = new TextBlock
            {
                Text = reason,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Colors.OrangeRed),
                FontWeight = Microsoft.UI.Text.FontWeights.Bold
            };

            TextBox txtNewLicense = new TextBox
            {
                Header = "Renovación de Licencia",
                PlaceholderText = "Ingresa la nueva clave (XXXX-XXXX-XXXX-XXXX)"
            };

            TextBlock statusBlock = new TextBlock
            {
                Text = "",
                FontSize = 12,
                Foreground = new SolidColorBrush(Colors.Red)
            };

            panel.Children.Add(msgBlock);
            panel.Children.Add(txtNewLicense);
            panel.Children.Add(statusBlock);

            ContentDialog dialog = new ContentDialog
            {
                Title = "⚠️ ACCESO RESTRINGIDO",
                Content = panel,
                PrimaryButtonText = "Validar y Renovar",
                CloseButtonText = "Cerrar Aplicación",
                XamlRoot = currentRoot
            };

            dialog.PrimaryButtonClick += async (s, args) =>
            {
                var deferral = args.GetDeferral();
                args.Cancel = true;

                statusBlock.Text = "Validando licencia...";
                statusBlock.Foreground = new SolidColorBrush(Colors.Gray);
                string inputKey = txtNewLicense.Text.Trim();

                if (string.IsNullOrEmpty(inputKey))
                {
                    statusBlock.Text = "Ingresa una licencia válida.";
                    statusBlock.Foreground = new SolidColorBrush(Colors.Red);
                    deferral.Complete();
                    return;
                }

                try
                {
                    var result = await SupabaseService.ValidateAndRegisterLicense(inputKey);

                    if (result.isValid)
                    {
                        SecurityService.UpdateLicenseKey(inputKey);

                        statusBlock.Foreground = new SolidColorBrush(Colors.Green);
                        statusBlock.Text = "¡Licencia renovada! Reanudando...";
                        await Task.Delay(1500);

                        args.Cancel = false;
                        _isRenewalDialogOpen = false;
                        InitializeSecurityWatchdog();
                    }
                    else
                    {
                        statusBlock.Foreground = new SolidColorBrush(Colors.Red);
                        statusBlock.Text = result.message ?? "Licencia no válida.";
                    }
                }
                catch (Exception ex)
                {
                    statusBlock.Text = "Error: " + ex.Message;
                }

                deferral.Complete();
            };

            dialog.CloseButtonClick += (s, args) =>
            {
                Application.Current.Exit();
            };

            await dialog.ShowAsync();
        }

        private void ContentFrame_Navigated(object sender, Microsoft.UI.Xaml.Navigation.NavigationEventArgs e) => NavView.IsBackEnabled = ContentFrame.CanGoBack;
        private void NavView_BackRequested(NavigationView sender, NavigationViewBackRequestedEventArgs args) { if (ContentFrame.CanGoBack) ContentFrame.GoBack(); }

        public void SetTitleBarTheme(ElementTheme theme)
        {
            var hWnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
            var wndId = Microsoft.UI.Win32Interop.GetWindowIdFromWindow(hWnd);
            var appWindow = Microsoft.UI.Windowing.AppWindow.GetFromWindowId(wndId);

            if (appWindow.TitleBar != null)
            {
                bool isDarkTheme = false;

                if (theme == ElementTheme.Dark)
                {
                    isDarkTheme = true;
                }
                else if (theme == ElementTheme.Default)
                {
                    isDarkTheme = Application.Current.RequestedTheme == ApplicationTheme.Dark;
                }

                appWindow.TitleBar.ButtonForegroundColor = isDarkTheme ? Colors.White : Colors.Black;
                appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
                appWindow.TitleBar.ButtonInactiveForegroundColor = Colors.Gray;

                appWindow.TitleBar.ButtonHoverBackgroundColor = isDarkTheme ?
                    Windows.UI.Color.FromArgb(25, 255, 255, 255) :
                    Windows.UI.Color.FromArgb(25, 0, 0, 0);
            }
        }

        private async void InitializeUpdateSystem()
        {
            string currentVersion = "1.0.0";
            var latestUpdate = await SupabaseService.CheckForUpdates();
            if (latestUpdate != null && IsNewerVersion(currentVersion, latestUpdate.Version))
            {
                await ShowUpdateDialog(latestUpdate);
            }

            await SupabaseService.StartUpdateWatchdog((newUpdate) =>
            {
                this.DispatcherQueue.TryEnqueue(async () =>
                {
                    if (IsNewerVersion(currentVersion, newUpdate.Version))
                    {
                        await ShowUpdateDialog(newUpdate);
                    }
                });
            });
        }

        private bool IsNewerVersion(string current, string remote)
        {
            try
            {
                Version v1 = new Version(current);
                Version v2 = new Version(remote);
                return v2 > v1;
            }
            catch
            {
                return false;
            }
        }

        private async Task ShowUpdateDialog(UpdateModel update)
        {
            if (_isRenewalDialogOpen) return;

            var currentRoot = this.Content?.XamlRoot;
            if (currentRoot == null) return;

            StackPanel panel = new StackPanel { Spacing = 10, Width = 400 };

            TextBlock title = new TextBlock
            {
                Text = $"¡Nueva versión {update.Version} disponible!",
                FontSize = 18,
                FontWeight = Microsoft.UI.Text.FontWeights.Bold,
                Foreground = new SolidColorBrush(Colors.DodgerBlue)
            };

            TextBlock notes = new TextBlock
            {
                Text = $"Novedades:\n{update.Notes}",
                TextWrapping = TextWrapping.Wrap,
                FontStyle = Windows.UI.Text.FontStyle.Italic,
                Margin = new Thickness(0, 10, 0, 10)
            };

            panel.Children.Add(title);
            panel.Children.Add(notes);

            ContentDialog dialog = new ContentDialog
            {
                Title = "ACTUALIZACIÓN DE SISTEMA",
                Content = panel,
                PrimaryButtonText = "Descargar Ahora",
                CloseButtonText = update.Mandatory ? null : "Después",
                XamlRoot = currentRoot
            };

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                try
                {
                    var uri = new Uri(update.DownloadUrl);
                    await Windows.System.Launcher.LaunchUriAsync(uri);
                }
                catch { }

                if (update.Mandatory) Application.Current.Exit();
            }
            else
            {
                if (update.Mandatory) Application.Current.Exit();
            }
        }
    }
}