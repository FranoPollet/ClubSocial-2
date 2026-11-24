using SQLite;

namespace ClubSocial.Models
{
    // Lugar donde entra o sale la plata del club: efectivo, una cuenta bancaria, etc.
    public class Cuenta
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // No puede haber dos cuentas con el mismo nombre
        [Indexed(Unique = true)]
        public string Nombre { get; set; } = string.Empty;
    }
}