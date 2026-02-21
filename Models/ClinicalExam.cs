using SQLite;

namespace OpticaPro.Models
{
    public class ClinicalExam
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public string PatientId { get; set; }
        public string Date { get; set; }

        // --- 1. ANAMNESIS ---
        public string MotivoConsulta { get; set; }
        public string HistoriaEnfermedad { get; set; }
        public string MedicacionActual { get; set; }

        // Lensometría (NUEVO)
        public string LensoSphOD { get; set; }
        public string LensoCylOD { get; set; }
        public string LensoAxisOD { get; set; }
        public string LensoAddOD { get; set; }
        public string LensoSphOI { get; set; }
        public string LensoCylOI { get; set; }
        public string LensoAxisOI { get; set; }
        public string LensoAddOI { get; set; }
        public string LensoTipoLente { get; set; }

        // Antecedentes (NUEVO: Guardaremos los checkboxes como texto separado por comas)
        public string AntecedentesSistemicos { get; set; } // Diabetes, etc.
        public string AntecedentesOculares { get; set; }   // Glaucoma fliar, etc.

        // --- 2. REFRACCIÓN ---
        public string SphereOD { get; set; }
        public string CylOD { get; set; }
        public string AxisOD { get; set; }
        public string AddOD { get; set; }
        public string SphereOI { get; set; }
        public string CylOI { get; set; }
        public string AxisOI { get; set; }
        public string AddOI { get; set; }

        public string AvOD { get; set; }
        public string AvOI { get; set; }
        public string AvscOD { get; set; }
        public string AvscOI { get; set; }
        public string Dip { get; set; }

        // Queratometría
        public string K1OD { get; set; }
        public string K2OD { get; set; }
        public string KAxisOD { get; set; }
        public string K1OI { get; set; }
        public string K2OI { get; set; }
        public string KAxisOI { get; set; }

        // --- 3. SALUD OCULAR ---
        public bool HasPterygiumOD { get; set; }
        public bool HasPterygiumOI { get; set; }
        public bool HasCataractOD { get; set; }
        public bool HasCataractOI { get; set; }
        public bool HasGlaucomaSuspicionOD { get; set; }
        public bool HasGlaucomaSuspicionOI { get; set; }

        // Fondo de Ojo (NUEVO)
        public string FondoExcavacion { get; set; }
        public string FondoMacula { get; set; }
        public string FondoRetina { get; set; }

        // --- 4. DIAGNÓSTICO Y PLAN ---
        public string DiagnosticoResumen { get; set; } // Miopía, Astigmatismo...
        public string PatologiaBinocular { get; set; } // Ambliopía, Estrabismo... (NUEVO)
        public string Observaciones { get; set; } // Plan de manejo

        // Sugerencias (NUEVO)
        public string SugerenciaDiseno { get; set; }
        public string SugerenciaMaterial { get; set; }
    }
}