using OpticaPro.Models;
using Supabase;
using Supabase.Realtime;
using Supabase.Realtime.PostgresChanges;
using System;
using System.Linq;
using System.Threading.Tasks;
using Client = Supabase.Client;

namespace OpticaPro.Services
{
    public static class SupabaseService
    {
        // TUS CREDENCIALES
        private const string SUPABASE_URL = "https://bcfisucsoqyugovcvujf.supabase.co";
        private const string SUPABASE_KEY = "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJpc3MiOiJzdXBhYmFzZSIsInJlZiI6ImJjZmlzdWNzb3F5dWdvdmN2dWpmIiwicm9sZSI6ImFub24iLCJpYXQiOjE3NjYwOTU1MzksImV4cCI6MjA4MTY3MTUzOX0.A-81B5FV3ZJ_6bX7Bpd7weJQ6yPj3ybdowZGO0E07uQ";

        public static Client Client { get; private set; }

        public static async Task InitializeAsync()
        {
            var options = new SupabaseOptions
            {
                AutoRefreshToken = true,
                AutoConnectRealtime = true
            };

            Client = new Client(SUPABASE_URL, SUPABASE_KEY, options);
            await Client.InitializeAsync();
        }

        // =========================================================================
        // === VIGILANTE DE ACTUALIZACIONES (NUEVO) ===
        // =========================================================================
        public static async Task StartUpdateWatchdog(Action<UpdateModel> onUpdateReceived)
        {
            try
            {
                // Escuchamos INSERTS en la tabla 'app_updates_windows'
                // Esto avisa apenas creas una fila nueva en Supabase
                var updateChannel = await Client.From<UpdateModel>()
                    .On(PostgresChangesOptions.ListenType.Inserts, (sender, change) =>
                    {
                        var newUpdate = change.Model<UpdateModel>();
                        if (newUpdate != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"ALERTA: Nueva actualización detectada v{newUpdate.Version}");
                            onUpdateReceived?.Invoke(newUpdate);
                        }
                    });

                await updateChannel.Subscribe();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error vigilante updates: {ex.Message}");
            }
        }

        // =========================================================================
        // === VIGILANTE DE SEGURIDAD 3.0 (CORREGIDO PARA FECHAS) ===
        // =========================================================================
        public static async Task StartLicenseWatchdog(string myLicenseKey, Action onLicenseRevoked)
        {
            try
            {
                string myHwId = SecurityService.GetMachineId();
                string myActivationId = "";

                // 1. Identificar mi activación actual
                var activationResp = await Client.From<ActivationModel>()
                    .Where(x => x.LicenseKey == myLicenseKey && x.HardwareId == myHwId)
                    .Get();

                var myActivation = activationResp.Models.FirstOrDefault();
                if (myActivation != null)
                {
                    myActivationId = myActivation.Id;
                }
                else
                {
                    // Si no estoy registrado, adiós
                    onLicenseRevoked?.Invoke();
                    return;
                }

                // -------------------------------------------------------------
                // CANAL 1: MONITOREO DE LICENCIA (Update y Delete)
                // -------------------------------------------------------------
                var licenseChannel = await Client.From<LicenseModel>()
                    .On(PostgresChangesOptions.ListenType.All, (sender, change) =>
                    {
                        // CASO A: Licencia eliminada por completo
                        if (change.Event == Supabase.Realtime.Constants.EventType.Delete)
                        {
                            var oldRow = change.OldModel<LicenseModel>();
                            if (oldRow != null && oldRow.LicenseKey == myLicenseKey)
                            {
                                System.Diagnostics.Debug.WriteLine("ALERTA: Licencia eliminada.");
                                onLicenseRevoked?.Invoke();
                            }
                        }

                        // CASO B: Licencia modificada (FECHA O ESTADO)
                        if (change.Event == Supabase.Realtime.Constants.EventType.Update)
                        {
                            var newRow = change.Model<LicenseModel>();
                            if (newRow != null && newRow.LicenseKey == myLicenseKey)
                            {
                                // 1. Verificar si fue baneada o desactivada
                                bool isBanned = newRow.Status == "banned" || newRow.Status == "expired" || !newRow.IsActive;

                                // 2. Verificar si la FECHA ya venció
                                bool isExpired = false;
                                if (newRow.Type != "PER")
                                {
                                    isExpired = newRow.ExpiryDate < DateTime.UtcNow;
                                }

                                if (isBanned || isExpired)
                                {
                                    System.Diagnostics.Debug.WriteLine("ALERTA: Licencia vencida o bloqueada en tiempo real.");
                                    onLicenseRevoked?.Invoke();
                                }
                            }
                        }
                    });

                await licenseChannel.Subscribe();

                // -------------------------------------------------------------
                // CANAL 2: MONITOREO DE MI DISPOSITIVO (Si me borran)
                // -------------------------------------------------------------
                var deviceChannel = await Client.From<ActivationModel>()
                    .On(PostgresChangesOptions.ListenType.Deletes, (sender, change) =>
                    {
                        var deletedRow = change.OldModel<ActivationModel>();

                        if (deletedRow != null)
                        {
                            if (deletedRow.Id == myActivationId || deletedRow.HardwareId == myHwId)
                            {
                                System.Diagnostics.Debug.WriteLine("ALERTA: Dispositivo eliminado.");
                                onLicenseRevoked?.Invoke();
                            }
                        }
                    });

                await deviceChannel.Subscribe();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error vigilante: {ex.Message}");
            }
        }

        // --- MÉTODOS AUXILIARES ---

        public static async Task<(bool isValid, string message, string clientName)> ValidateAndRegisterLicense(string licenseKey)
        {
            try
            {
                string hwId = SecurityService.GetMachineId();
                string deviceName = Environment.MachineName;

                var response = await Client.From<LicenseModel>().Where(x => x.LicenseKey == licenseKey).Get();
                var license = response.Models.FirstOrDefault();

                if (license == null) return (false, "Licencia no encontrada.", null);
                if (license.Status == "banned") return (false, "Licencia bloqueada por el administrador.", null);

                DateTime currentExpiry = license.ExpiryDate;
                if (license.Status == "unused")
                {
                    DateTime now = DateTime.UtcNow;
                    if (licenseKey.Contains("-MEN-")) currentExpiry = now.AddMonths(1);
                    else if (licenseKey.Contains("-ANU-")) currentExpiry = now.AddYears(1);
                    else if (licenseKey.Contains("-PER-")) currentExpiry = now.AddYears(100);
                    else currentExpiry = now.AddMonths(1);

                    await Client.From<LicenseModel>().Where(x => x.Id == license.Id)
                        .Set(x => x.Status, "active").Set(x => x.ExpiryDate, currentExpiry).Update();
                }

                if (currentExpiry < DateTime.UtcNow) return (false, "La licencia ha expirado.", null);

                var activationResponse = await Client.From<ActivationModel>().Where(x => x.LicenseKey == licenseKey).Get();
                var activations = activationResponse.Models;

                var currentDevice = activations.FirstOrDefault(x => x.HardwareId == hwId);
                if (currentDevice != null) return (true, "Licencia activa.", license.ClientName);

                if (activations.Count >= license.MaxDevices) return (false, $"Límite de dispositivos alcanzado ({activations.Count}/{license.MaxDevices}).", null);

                var newActivation = new ActivationModel
                {
                    LicenseKey = licenseKey,
                    HardwareId = hwId,
                    DeviceName = deviceName,
                    ActivatedAt = DateTime.UtcNow
                };
                await Client.From<ActivationModel>().Insert(newActivation);

                return (true, "Activación exitosa.", license.ClientName);
            }
            catch (Exception ex)
            {
                return (false, $"Error de conexión: {ex.Message}", null);
            }
        }

        public static async Task SendMessage(string licenseKey, string content)
        {
            var msg = new MessageModel { LicenseKey = licenseKey, Content = content, IsAdmin = false, CreatedAt = DateTime.UtcNow };
            await Client.From<MessageModel>().Insert(msg);
        }

        public static async Task<UpdateModel> CheckForUpdates()
        {
            try
            {
                var result = await Client.From<UpdateModel>()
                    .Order("created_at", Supabase.Postgrest.Constants.Ordering.Descending)
                    .Limit(1)
                    .Get();
                return result.Models.FirstOrDefault();
            }
            catch { return null; }
        }

        public static async Task UpdateClientName(string licenseKey, string businessName)
        {
            try { await Client.From<LicenseModel>().Where(x => x.LicenseKey == licenseKey).Set(x => x.ClientName, businessName).Update(); } catch { }
        }
    }
}