using OpticaPro.Models;
using System.Collections.Generic;
using System.Linq;
using System;

namespace OpticaPro.Services
{
    public static class PatientRepository
    {
        static PatientRepository()
        {
            DatabaseService.Initialize();
        }

        // ---------------------------------------------------------
        // PEDIDOS (ORDERS)
        // ---------------------------------------------------------
        public static void SaveOrder(Order order)
        {
            var db = DatabaseService.GetConnection();

            // Validación de seguridad: No guardar sin cliente
            if (string.IsNullOrEmpty(order.PatientId))
                throw new Exception("Error: El pedido no tiene un cliente asignado.");

            // Aseguramos que la fecha esté establecida si viene vacía
            if (string.IsNullOrEmpty(order.Date))
                order.Date = DateTime.Now.ToString("dd/MM/yyyy");

            if (order.Id != 0)
            {
                db.Update(order);
            }
            else
            {
                db.Insert(order);
            }
        }

        public static List<Order> GetOrdersByPatientId(string patientId)
        {
            try
            {
                return DatabaseService.GetConnection()
                         .Table<Order>()
                         .Where(o => o.PatientId == patientId)
                         .OrderByDescending(o => o.Id)
                         .ToList();
            }
            catch
            {
                return new List<Order>();
            }
        }

        public static List<Order> GetAllOrders()
        {
            try
            {
                return DatabaseService.GetConnection().Table<Order>().OrderByDescending(o => o.Id).ToList();
            }
            catch
            {
                return new List<Order>();
            }
        }

        // ---------------------------------------------------------
        // EXÁMENES CLÍNICOS (ClinicalExam) - ¡AGREGADO!
        // ---------------------------------------------------------
        public static void SaveClinicalExam(ClinicalExam exam)
        {
            var db = DatabaseService.GetConnection();

            if (string.IsNullOrEmpty(exam.PatientId))
                throw new Exception("No se puede guardar un examen sin paciente.");

            if (exam.Id != 0)
            {
                db.Update(exam);
            }
            else
            {
                db.Insert(exam);
            }
        }

        public static List<ClinicalExam> GetExamsByPatientId(string patientId)
        {
            return DatabaseService.GetConnection()
                     .Table<ClinicalExam>()
                     .Where(x => x.PatientId == patientId)
                     .OrderByDescending(x => x.Id)
                     .ToList();
        }

        // ---------------------------------------------------------
        // PACIENTES (PATIENTS) - ¡AGREGADO DELETE!
        // ---------------------------------------------------------
        public static List<Patient> GetAllPatients()
        {
            return DatabaseService.GetConnection().Table<Patient>().OrderBy(p => p.FullName).ToList();
        }

        public static void AddPatient(Patient patient)
        {
            // Asegurar ID si viene vacío
            if (string.IsNullOrEmpty(patient.Id)) patient.Id = Guid.NewGuid().ToString();
            DatabaseService.GetConnection().Insert(patient);
        }

        public static void UpdatePatient(Patient patient) => DatabaseService.GetConnection().Update(patient);

        // Esta es la función que te faltaba para eliminar
        public static void DeletePatient(Patient patient)
        {
            var db = DatabaseService.GetConnection();

            // Opcional: Eliminar datos relacionados para limpiar la BD
            // db.Table<Order>().Delete(x => x.PatientId == patient.Id);
            // db.Table<ClinicalExam>().Delete(x => x.PatientId == patient.Id);

            db.Delete(patient);
        }
    }
}