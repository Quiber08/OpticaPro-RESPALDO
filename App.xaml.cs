using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Navigation;
using OpticaPro.Views;
using OpticaPro.Services;
using System;
using System.Threading.Tasks;

namespace OpticaPro
{
    public partial class App : Application
    {
        public static Window m_window;

        public App()
        {
            this.InitializeComponent();
            // Manejador global de excepciones para errores no controlados
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Evita que la app se cierre de golpe si es posible
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"ERROR NO CONTROLADO: {e.Exception.Message}");
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // 1. INICIALIZAR BASE DE DATOS SQLITE
                // Envolvemos en try/catch específico para detectar fallos de DLL
                try
                {
                    DatabaseService.Initialize();
                }
                catch (Exception dbEx)
                {
                    throw new Exception($"Error al cargar base de datos (SQLite): {dbEx.Message}", dbEx);
                }

                // 2. INICIALIZAR SUPABASE
                await SupabaseService.InitializeAsync();

                // 3. ABRIR VENTANA DE LOGIN
                m_window = new LoginWindow();
                m_window.Activate();
            }
            catch (Exception ex)
            {
                // SI OCURRE UN ERROR FATAL, MOSTRAMOS DIÁLOGO DE EMERGENCIA
                EnsureWindow();

                var dialog = new ContentDialog
                {
                    Title = "Error Fatal al Iniciar",
                    Content = $"La aplicación no pudo arrancar.\n\nError: {ex.Message}\n\nPosible causa: {ex.InnerException?.Message ?? "Desconocida"}",
                    CloseButtonText = "Cerrar",
                    XamlRoot = m_window.Content?.XamlRoot
                };

                // Si XamlRoot es nulo (ventana no cargada), forzamos un mensaje en debug
                System.Diagnostics.Debug.WriteLine($"CRASH: {ex.ToString()}");

                try
                {
                    if (dialog.XamlRoot != null) await dialog.ShowAsync();
                }
                catch { }
            }
        }

        private void EnsureWindow()
        {
            if (m_window == null)
            {
                m_window = new Window();
                m_window.Activate();
            }
        }
    }
}