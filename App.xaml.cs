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
        
        // VARIABLE GLOBAL: Para controlar la ventana de WhatsApp desde cualquier lado
        public static MarketingWindow MarketingWindowInstance { get; set; }

        public App()
        {
            this.InitializeComponent();
            this.UnhandledException += App_UnhandledException;
        }

        private void App_UnhandledException(object sender, Microsoft.UI.Xaml.UnhandledExceptionEventArgs e)
        {
            // Evitamos que la app se cierre de golpe por errores desconocidos
            e.Handled = true;
            System.Diagnostics.Debug.WriteLine($"ERROR NO CONTROLADO: {e.Exception.Message}");
        }

        protected override async void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            try
            {
                // 1. INICIALIZAR BASE DE DATOS LOCAL (SQLite)
                try
                {
                    DatabaseService.Initialize();
                }
                catch (Exception dbEx)
                {
                    // Si falla la BD local, intentamos seguir, pero avisamos en consola
                    System.Diagnostics.Debug.WriteLine($"Error BD Local: {dbEx.Message}");
                }

                // 2. INICIALIZAR SUPABASE (Nube)
                try
                {
                    await SupabaseService.InitializeAsync();
                }
                catch (Exception cloudEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error Supabase: {cloudEx.Message}");
                }

                // 3. ABRIR SOLAMENTE LA VENTANA DE LOGIN
                // (No abrimos Marketing todavía para que el inicio sea limpio)
                m_window = new LoginWindow();
                m_window.Activate();
            }
            catch (Exception ex)
            {
                // Si todo falla (Crash crítico al inicio)
                EnsureWindow();
                
                var dialog = new ContentDialog
                {
                    Title = "Error Fatal",
                    Content = $"No se pudo iniciar la aplicación.\nError: {ex.Message}",
                    CloseButtonText = "Salir",
                    XamlRoot = m_window.Content?.XamlRoot
                };
                
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