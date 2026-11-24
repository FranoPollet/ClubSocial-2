using SQLite;

namespace ClubSocial.Models
{
    // guardo una copia de los valores del momento.
    public class Gasto
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public DateTime Fecha { get; set; }

        // Primer día del mes del gasto, para buscarlos por mes
        [Indexed]
        public DateTime Periodo { get; set; }

        public string Descripcion { get; set; } = string.Empty;

        public decimal Monto { get; set; }

        public Moneda Moneda { get; set; }

        // Valor del dólar usado (0 si el gasto fue en pesos)
        public decimal Cotizacion { get; set; }

        // Lo que equivale en pesos
        public decimal MontoEnPesos { get; set; }

        // Cuenta de la que salió la plata
        public int CuentaId { get; set; }
    }
}