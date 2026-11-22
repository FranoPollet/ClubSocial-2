using ClubSocial.Models;

namespace ClubSocial.ViewModels
{
    // Un socio junto con el estado de su cuota en el mes elegido, para mostrar en la lista de cobranza
    public class SocioCobranza
    {
        public Socio Socio { get; set; } = new Socio();

        public Categoria? Categoria { get; set; }

        // El pago de la cuota del mes (null si todavía no pagó)
        public Pago? PagoCuota { get; set; }

        public int CantidadActividades { get; set; }

        public string NombreCompleto => $"{Socio.Apellido}, {Socio.Nombre}";

        // Texto sobre el que busca el filtro
        public string TextoBusqueda => $"{Socio.Nombre} {Socio.Apellido} {Socio.Dni}";

        public string CategoriaTexto => Categoria == null ? "Sin categoría" : Categoria.Nombre;

        public bool CuotaPagada => PagoCuota != null;

        public bool CuotaPendiente => PagoCuota == null;

        public string CuotaTexto => PagoCuota == null
            ? "Cuota: pendiente"
            : $"Cuota: pagada el {PagoCuota.FechaPago:dd/MM/yyyy}";

        public string ActividadesTexto => $"Actividades cobradas en el mes: {CantidadActividades}";
    }
}