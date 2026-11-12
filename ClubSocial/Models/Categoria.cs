using SQLite;

namespace ClubSocial.Models
{
    public class Categoria
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        //No puede haber 2 categorías con el mismo nombre
        [Indexed(Unique = true)]
        public string Nombre { get; set; } = string.Empty;

        //Si es true, la cuota incluye una actividad obligatorio por mes (El vale obligatorio mensual)
        public bool IncluyeVale { get; set; }
    }
}