using ClubSocial.Models;

namespace ClubSocial.ViewModels
{
    // Una categoría junto con el texto de su precio actual, para mostrar en la lista
    public class CategoriaConPrecio
    {
        public Categoria Categoria { get; set; } = new Categoria();

        public string Nombre => Categoria.Nombre;

        public string ValeTexto => Categoria.IncluyeVale
            ? "Incluye vale de actividad"
            : "Sin vale";

        public string PrecioTexto { get; set; } = "Sin precio cargado";
    }
}