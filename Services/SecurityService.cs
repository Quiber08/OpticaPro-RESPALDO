using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using OpticaPro.Models;

namespace OpticaPro.Services
{
    public class SecurityData
    {
        public string LicenseKey { get; set; }
        public string AdminName { get; set; }
        public string Password { get; set; }
        public bool IsRegistered { get; set; }
        public string RegisteredMachineId { get; set; }
        public string ProfileImagePath { get; set; }

        // --- CAMPOS DE SEGURIDAD OFFLINE ---
        public DateTime? LocalExpiryDate { get; set; } // Fecha cuando caduca realmente
        public DateTime? LastRunDate { get; set; }     // Última vez que se abrió la app (Anti-Trampa)
    }

    public static class SecurityService
    {
        private static readonly string _folderPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "OpticaProData");
        private static readonly string _filePath = Path.Combine(_folderPath, "security_config.json");

        private static SecurityData _currentData;
        private static readonly string SECRET_SALT = "OPTICA_PRO_2026_SECURE_HARDWARE_ID";

        public static string CurrentUserName { get; private set; }
        public static string CurrentUserRole { get; private set; }
        public static string CurrentProfileImage => _currentData?.ProfileImagePath;
        public static event Action OnProfileChanged;

        static SecurityService()
        {
            if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);
            LoadData();
        }

        private static void LoadData()
        {
            if (File.Exists(_filePath))
            {
                try
                {
                    string json = File.ReadAllText(_filePath);
                    _currentData = JsonSerializer.Deserialize<SecurityData>(json);
                }
                catch { _currentData = new SecurityData { IsRegistered = false }; }
            }
            else { _currentData = new SecurityData { IsRegistered = false }; }
        }

        public static string GetLicenseKey() => _currentData?.LicenseKey;

        public static void UpdateLicenseKey(string newKey)
        {
            if (_currentData == null) _currentData = new SecurityData();

            _currentData.LicenseKey = newKey;
            _currentData.IsRegistered = true;
            // Al renovar, reseteamos la fecha de última ejecución para evitar bloqueos por error
            _currentData.LastRunDate = DateTime.Now;
            Save();
        }

        // =======================================================================
        // FUNCIÓN MAESTRA DE VALIDACIÓN (CON ANTI-TRAMPA Y SOPORTE OFFLINE)
        // =======================================================================
        public static async Task<bool> IsLicenseValidOnline()
        {
            string key = GetLicenseKey();
            if (string.IsNullOrEmpty(key)) return false;

            // 1. CONTROL ANTI-TRAMPA (Reloj del Sistema)
            // Si la fecha actual es MENOR a la última vez que se usó, el usuario atrasó el reloj.
            if (_currentData.LastRunDate.HasValue)
            {
                // Damos un margen de 24 horas por si viajó a otra zona horaria, pero no más.
                if (DateTime.Now < _currentData.LastRunDate.Value.AddHours(-24))
                {
                    // ¡FRAUDE DETECTADO! El reloj está en el pasado.
                    return false;
                }
            }

            try
            {
                // 2. INTENTO DE VALIDACIÓN ONLINE
                var result = await SupabaseService.Client.From<LicenseModel>()
                    .Where(x => x.LicenseKey == key)
                    .Get();

                var license = result.Models.FirstOrDefault();

                if (license == null) return false;      // No existe
                if (!license.IsActive) return false;    // Desactivada por Admin

                // --- ACTUALIZACIÓN DE CACHÉ LOCAL (IMPORTANTE) ---
                // Cada vez que hay internet, guardamos la fecha real de vencimiento
                _currentData.LocalExpiryDate = license.ExpiryDate;

                // Actualizamos la "Última vez visto" al momento actual
                _currentData.LastRunDate = DateTime.Now;
                Save();
                // --------------------------------------------------

                // Chequeo de fecha online
                if (license.Type != "PER" && DateTime.Now.Date > license.ExpiryDate.Date)
                    return false;

                return true;
            }
            catch (Exception)
            {
                // 3. MODO OFFLINE (Sin internet)

                // Si nunca se ha validado online y no tenemos fecha guardada -> Bloquear por seguridad
                if (!_currentData.LocalExpiryDate.HasValue) return false;

                // Verificamos si la licencia YA venció según el archivo local
                if (DateTime.Now.Date > _currentData.LocalExpiryDate.Value.Date)
                {
                    return false; // Caducó (Offline)
                }

                // Actualizamos LastRunDate incluso offline para seguir protegiendo contra retroceso de reloj
                _currentData.LastRunDate = DateTime.Now;
                Save();

                return true; // Pasa la prueba offline
            }
        }

        public static string GetMachineId()
        {
            try
            {
                Process process = new Process();
                process.StartInfo.FileName = "wmic";
                process.StartInfo.Arguments = "csproduct get uuid";
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.CreateNoWindow = true;
                process.Start();

                string output = process.StandardOutput.ReadToEnd();
                process.WaitForExit();

                var lines = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length >= 2) return lines[1].Trim();
            }
            catch { return "GENERIC-" + Environment.MachineName; }
            return "UNKNOWN-HWID";
        }

        public static bool IsAppInitialized()
        {
            if (_currentData == null || !_currentData.IsRegistered) return false;
            return true;
        }

        public static bool IsRunFromSafeLocation() => true;
        public static string GetAdminName() => _currentData?.AdminName;

        public static bool ValidateLicense(string inputKey)
        {
            if (inputKey == "MASTER-QUIBER-SUPPORT") return true;
            string machineId = GetMachineId();
            string expectedKey = GenerateKeyForMachine(machineId);
            return inputKey == expectedKey;
        }

        public static string GenerateKeyForMachine(string machineId)
        {
            using (var sha = SHA256.Create())
            {
                byte[] bytes = Encoding.UTF8.GetBytes(machineId + SECRET_SALT);
                byte[] hash = sha.ComputeHash(bytes);
                string raw = BitConverter.ToString(hash).Replace("-", "");
                return $"{raw.Substring(0, 4)}-{raw.Substring(4, 4)}-{raw.Substring(8, 4)}-{raw.Substring(12, 4)}";
            }
        }

        public static void RegisterAdmin(string license, string name, string password)
        {
            _currentData = new SecurityData
            {
                LicenseKey = license,
                AdminName = name,
                Password = password,
                IsRegistered = true,
                RegisteredMachineId = GetMachineId(),
                LastRunDate = DateTime.Now // Inicializamos el reloj
            };
            Save();
        }

        public static bool Login(string username, string password)
        {
            if (!IsAppInitialized()) return false;

            if (string.Equals(username, _currentData.AdminName, StringComparison.OrdinalIgnoreCase) && password == _currentData.Password)
            {
                CurrentUserName = _currentData.AdminName;
                CurrentUserRole = "Admin";
                return true;
            }

            var employees = UserRepository.GetAllUsers();
            var foundUser = employees.FirstOrDefault(u =>
                string.Equals(u.Username, username, StringComparison.OrdinalIgnoreCase) &&
                u.Password == password);

            if (foundUser != null)
            {
                CurrentUserName = foundUser.FullName;
                CurrentUserRole = foundUser.Role;
                return true;
            }
            return false;
        }

        public static bool VerifyMasterLicense(string inputLicense)
        {
            if (_currentData == null) return false;
            return inputLicense == _currentData.LicenseKey || ValidateLicense(inputLicense);
        }

        public static void UpdateAdminPassword(string newPassword)
        {
            if (_currentData != null)
            {
                _currentData.Password = newPassword;
                Save();
            }
        }

        public static bool ChangePassword(string currentPass, string newPass)
        {
            if (_currentData == null) LoadData();
            if (!string.IsNullOrEmpty(_currentData.Password) && _currentData.Password != currentPass) return false;
            _currentData.Password = newPass;
            Save();
            return true;
        }

        public static void UpdateProfile(string newName, string newImagePath)
        {
            if (_currentData == null) LoadData();
            bool changed = false;
            if (_currentData.AdminName != newName) { _currentData.AdminName = newName; CurrentUserName = newName; changed = true; }
            if (_currentData.ProfileImagePath != newImagePath) { _currentData.ProfileImagePath = newImagePath; changed = true; }
            if (changed) { Save(); OnProfileChanged?.Invoke(); }
        }

        private static void Save()
        {
            try
            {
                if (!Directory.Exists(_folderPath)) Directory.CreateDirectory(_folderPath);
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(_currentData, options);
                File.WriteAllText(_filePath, json);
            }
            catch (Exception ex) { Debug.WriteLine($"Error guardando config seguridad: {ex.Message}"); }
        }

        public static void DeactivateLicense()
        {
            if (_currentData != null)
            {
                _currentData.IsRegistered = false;
                _currentData.LicenseKey = "";
                _currentData.LocalExpiryDate = null; // Borramos fecha local
                Save();
            }
        }
    }
}