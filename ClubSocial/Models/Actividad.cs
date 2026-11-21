using SQLite;

namespace ClubSocial.Models
{
    public class Actividad
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        // No puede haber dos actividades con el mismo nombre
        [Indexed(Unique = true)]
        public string Nombre { get; set; } = string.Empty;

        // Si una actividad ya no se dicta la desactivo, así no pierdo los pagos viejos
        public bool Activa { get; set; } = true;
    }
}
