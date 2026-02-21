namespace OpticaPro.Models
{
    public class StatCard
    {
        public string Title { get; set; }  // Ej: "Pacientes"
        public string Value { get; set; }  // Ej: "12"
        public string Icon { get; set; }   // Ej: "\uE8A3" (El dibujito)
        public string Trend { get; set; }  // Ej: "+5% vs ayer"
    }

    public class RecentPatient
    {
        public string Name { get; set; }
        public string Date { get; set; }
        public string Status { get; set; }
        public string StatusColor { get; set; }
    }
}