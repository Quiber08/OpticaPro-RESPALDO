using System;
using System.Threading.Tasks;
using Windows.System;

namespace OpticaPro.Services
{
    public static class WhatsAppService
    {
        public static async Task<bool> SendMessageAsync(string phoneNumber, string message)
        {
            if (string.IsNullOrWhiteSpace(phoneNumber)) return false;

            // 1. Limpieza del número (Solo dígitos)
            string cleanNumber = new string(System.Linq.Enumerable.ToArray(System.Linq.Enumerable.Where(phoneNumber, char.IsDigit)));

            // 2. Ajuste de prefijo (Ecuador: Si es 09... lo cambia a 5939...)
            if (cleanNumber.Length == 10 && cleanNumber.StartsWith("09"))
            {
                cleanNumber = "593" + cleanNumber.Substring(1);
            }

            // 3. Codificar mensaje para URL
            string urlMessage = Uri.EscapeDataString(message);

            // --- INTENTO 1: APP DE ESCRITORIO ---
            // Usamos el protocolo whatsapp:// que busca la app instalada
            string appUriString = $"whatsapp://send?phone={cleanNumber}&text={urlMessage}";

            try
            {
                // Intentamos abrir la App
                var appUri = new Uri(appUriString);

                // LaunchUriAsync devuelve true si encontró la app, false si no.
                bool appLaunched = await Launcher.LaunchUriAsync(appUri);

                if (appLaunched)
                {
                    return true; // Éxito, se abrió la app
                }
            }
            catch
            {
                // Si falla por cualquier razón, ignoramos y pasamos al plan B
            }

            // --- INTENTO 2: WHATSAPP WEB (NAVEGADOR) ---
            // Si llegamos aquí es porque la App falló. Abrimos el navegador.
            string webUriString = $"https://web.whatsapp.com/send?phone={cleanNumber}&text={urlMessage}";

            try
            {
                var webUri = new Uri(webUriString);
                return await Launcher.LaunchUriAsync(webUri);
            }
            catch
            {
                return false; // Falló todo (no hay navegador??)
            }
        }
    }
}