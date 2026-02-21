using System;
using SQLite;
using OpticaPro.Models;

namespace OpticaPro.Services
{
    // 1. CONVERTIMOS LA CLASE EN UNA TABLA DE BASE DE DATOS
    public class AppSettings
    {
        [PrimaryKey]
        public int Id { get; set; } = 1; // Solo existirá una fila con ID 1

        // --- DATOS DE LA CLÍNICA ---
        public string ClinicName { get; set; } = "";
        public string ClinicAddress { get; set; } = "";
        public string ClinicPhone { get; set; } = "";
        public string ClinicRuc { get; set; } = "";

        // Nuevos campos
        public string ClinicEmail { get; set; } = "";
        public string ClinicCity { get; set; } = "";
        public string ClinicSlogan { get; set; } = "";

        // --- DATOS DEL PROFESIONAL ---
        public string DoctorName { get; set; } = "";
        public string DoctorSpecialty { get; set; } = "Optometrista";
        public string DoctorLicense { get; set; } = "";
    }

    public static class SettingsService
    {
        private static AppSettings _current;

        // EVENTO MEGÁFONO (Lo mantenemos porque MainWindow lo necesita)
        public static event Action<string> OnClientNameChanged;

        // Constructor estático: Se asegura que la tabla exista al arrancar
        static SettingsService()
        {
            try
            {
                var db = DatabaseService.GetConnection();
                db.CreateTable<AppSettings>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error iniciando SettingsService: {ex.Message}");
            }
        }

        public static AppSettings Current
        {
            get
            {
                if (_current == null) Load();
                return _current;
            }
        }

        // MÉTODO DE CARGA (Ahora lee de SQLite, no de un archivo de texto)
        public static void Load()
        {
            try
            {
                var db = DatabaseService.GetConnection();
                // Buscamos la configuración ID=1
                _current = db.Table<AppSettings>().FirstOrDefault(x => x.Id == 1);

                // Si no existe (primera vez), creamos una vacía
                if (_current == null)
                {
                    _current = new AppSettings { Id = 1, ClinicName = "Mi Óptica" };
                    db.Insert(_current);
                }
            }
            catch
            {
                // Fallback de emergencia
                _current = new AppSettings { Id = 1 };
            }
        }

        // MÉTODO DE GUARDADO (Ahora es seguro y a prueba de fallos)
        public static void Save()
        {
            try
            {
                var db = DatabaseService.GetConnection();
                // InsertOrReplace: Si existe lo actualiza, si no lo crea.
                db.InsertOrReplace(_current);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error guardando en SQLite: {ex.Message}");
            }
        }

        // --- MÉTODOS DE COMPATIBILIDAD CON TU MAINWINDOW ---

        public static void SetClientName(string newName)
        {
            // 1. Actualizar memoria
            Current.ClinicName = newName;

            // 2. Guardar en Base de Datos
            Save();

            // 3. Avisar a la ventana para que cambie el título
            OnClientNameChanged?.Invoke(newName);
        }

        public static string GetClientName()
        {
            return Current.ClinicName;
        }
    }
}