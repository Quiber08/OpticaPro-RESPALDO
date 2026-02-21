using SQLite;
using OpticaPro.Models;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System;

namespace OpticaPro.Services
{
    public static class AppointmentRepository
    {
        private static SQLiteConnection _db;

        public static void Initialize()
        {
            if (_db == null)
            {
                var dbPath = Path.Combine(System.Environment.GetFolderPath(System.Environment.SpecialFolder.LocalApplicationData), "opticapro.db3");
                _db = new SQLiteConnection(dbPath);
                _db.CreateTable<Appointment>();
            }
        }

        // Para la página principal de Citas
        public static List<Appointment> GetAllAppointments()
        {
            Initialize();
            return _db.Table<Appointment>().ToList();
        }

        // --- ESTE ES EL MÉTODO QUE FALTABA PARA EL HISTORIAL ---
        public static List<Appointment> GetByPatient(string patientId)
        {
            Initialize();
            // Buscamos por ID de paciente para mayor precisión
            return _db.Table<Appointment>().Where(a => a.PatientId == patientId).ToList();
        }

        public static void AddAppointment(Appointment appointment)
        {
            Initialize();
            _db.Insert(appointment);
        }

        public static void UpdateAppointment(Appointment appointment)
        {
            Initialize();
            _db.Update(appointment);
        }

        public static void DeleteAppointment(Appointment appointment)
        {
            Initialize();
            _db.Delete(appointment);
        }
    }
}