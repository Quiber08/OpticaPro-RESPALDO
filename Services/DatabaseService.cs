using System;
using System.IO;
using SQLite;
using OpticaPro.Models;

namespace OpticaPro.Services
{
    public static class DatabaseService
    {
        private static readonly string DbName = "optica_pro.db3";
        private static readonly string DbPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), DbName);
        private static SQLiteConnection _connection;

        public static void Initialize()
        {
            if (_connection != null) return;

            try
            {
                // Crear directorio si no existe
                var dir = Path.GetDirectoryName(DbPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                _connection = new SQLiteConnection(DbPath);

                // Crear tablas (si no existen)
                _connection.CreateTable<Patient>();
                _connection.CreateTable<Order>();
                _connection.CreateTable<Product>();
                _connection.CreateTable<ClinicalExam>();
                _connection.CreateTable<Appointment>();
                // Agrega aquí otras tablas si tienes (Users, etc.)
                // _connection.CreateTable<AppUser>(); 

                // --- MIGRACIÓN / REPARACIÓN ---
                // Verifica si la columna PatientId existe en la tabla Order.
                // SQLite no tira error si intentas agregar una columna que ya existe, 
                // pero por seguridad lo envolvemos en try/catch.
                try
                {
                    // Intentamos agregar la columna por si es una versión vieja de la BD
                    var tableInfo = _connection.GetTableInfo("Order");
                    bool exists = false;
                    foreach (var col in tableInfo)
                    {
                        if (col.Name == "PatientId") { exists = true; break; }
                    }

                    if (!exists)
                    {
                        _connection.Execute("ALTER TABLE \"Order\" ADD COLUMN PatientId varchar");
                    }
                }
                catch (Exception migrationEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Error menor en migración: {migrationEx.Message}");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"ERROR CRÍTICO BD: {ex.Message}");
            }
        }

        public static SQLiteConnection GetConnection()
        {
            if (_connection == null) Initialize();
            return _connection;
        }
    }
}