using SQLite;

namespace ClubSocial.Models
{
    // Cada fila es tipo desde hoy en adelante esto cuesta tanto. entonces cuando subimos los precios no se modifica la fila vieja, sino que agregamos una nueva.
    public class Precio
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public TipoConcepto Tipo { get; set; }

        // Id de la categoría o actividad (0 cuando es la inscripción)
        public int ConceptoId { get; set; }

        public decimal Monto { get; set; }

        public Moneda Moneda { get; set; }

        //El precio rige desde esta fecha en adelante
        public DateTime VigenteDesde { get; set; }
    }
}