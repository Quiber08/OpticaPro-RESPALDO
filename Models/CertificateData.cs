using System;

namespace OpticaPro.Models
{
    public class CertificateData
    {
        // Paciente
        public string PatientName { get; set; }
        public string PatientAge { get; set; }
        public string PatientHistory { get; set; }
        public string Date { get; set; }

        // Clínica
        public string ClinicName { get; set; }
        public string ClinicAddress { get; set; }
        public string ClinicPhone { get; set; }
        public string ClinicRuc { get; set; }

        // Doctor
        public string DoctorName { get; set; }
        public string DoctorSpecialty { get; set; }
        public string DoctorLicense { get; set; }

        // Refracción
        public string SphereOD { get; set; }
        public string CylOD { get; set; }
        public string AxisOD { get; set; }
        public string AddOD { get; set; } // [NUEVO]
        public string AvOD { get; set; }

        public string SphereOI { get; set; }
        public string CylOI { get; set; }
        public string AxisOI { get; set; }
        public string AddOI { get; set; } // [NUEVO]
        public string AvOI { get; set; }

        public string Diagnosis { get; set; }
        public string Recommendation { get; set; }
    }
}