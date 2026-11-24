using SQLite;

namespace ClubSocial.Models
{
    // Un cobro hecho a un socio. Guarda una copia de los valores del momento,
    // así si después cambian los precios o el dólar, los pagos viejos no se tocan.
    public class Pago
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed]
        public int SocioId { get; set; }

        // Cuota o Actividad
        public TipoConcepto Tipo { get; set; }

        // Id de la categoría (si es cuota) o de la actividad
        public int ConceptoId { get; set; }

        // Primer día del mes que se está pagando
        public DateTime Periodo { get; set; }

        // Día en que se cobró (lo carga el usuario)
        public DateTime FechaPago { get; set; }

        // Precio y moneda que tenía en ese momento
        public decimal Monto { get; set; }

        public Moneda Moneda { get; set; }

        // Valor del dólar usado (0 si el precio estaba en pesos)
        public decimal Cotizacion { get; set; }

        // Lo que equivale en pesos
        public decimal MontoEnPesos { get; set; }

        // true si se pagó con el vale incluido en la cuota
        public bool ConVale { get; set; }

        // Cuenta en la que entró la plata (0 = sin cuenta asignada)
        public int CuentaId { get; set; }

        // Texto guardado, por si después se renombra la categoría o la actividad
        public string Detalle { get; set; } = string.Empty;
    }
}