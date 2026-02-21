using SQLite;

namespace OpticaPro.Models
{
    public class AppUser
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string FullName { get; set; }
        public string Username { get; set; }
        public string Password { get; set; } // En producción deberías encriptarla
        public string Role { get; set; } // "Vendedor", "Optometrista", "Admin"

        // Propiedad auxiliar para el diseño (Iniciales)
        [Ignore]
        public string Initials
        {
            get
            {
                if (string.IsNullOrEmpty(FullName)) return "U";
                var parts = FullName.Split(' ');
                if (parts.Length > 1)
                    return $"{parts[0][0]}{parts[1][0]}".ToUpper();
                return FullName.Substring(0, 1).ToUpper();
            }
        }
    }
}