using ClubSocial.Models;

namespace ClubSocial.ViewModels
{
    // Una categoría o una actividad junto con el texto de su precio actual, para mostrar en las listas
    public class ConceptoConPrecio
    {
        // Cuota (si es una categoría) o Actividad
        public TipoConcepto Tipo { get; set; }

        public int Id { get; set; }

        public string Nombre { get; set; } = string.Empty;

        // Texto de apoyo, por ejemplo si la categoría incluye vale
        public string DetalleTexto { get; set; } = string.Empty;

        public string PrecioTexto { get; set; } = "Sin precio cargado";

        // Guardo el objeto original para poder eliminarlo (solo uno de los dos tiene valor)
        public Categoria? Categoria { get; set; }

        public Actividad? Actividad { get; set; }
    }
}