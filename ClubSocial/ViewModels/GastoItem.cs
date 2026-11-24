using ClubSocial.Models;

namespace ClubSocial.ViewModels
{
    // Un gasto con los textos ya armados para mostrar en la lista
    public class GastoItem
    {
        public Gasto Gasto { get; set; } = new Gasto();

        public string CuentaTexto { get; set; } = "Sin cuenta";

        public string Descripcion => Gasto.Descripcion;

        public string FechaTexto => $"{Gasto.Fecha:dd/MM/yyyy} - {CuentaTexto}";

        public string MontoTexto
        {
            get
            {
                if (Gasto.Moneda == Moneda.USD)
                {
                    return $"USD {Gasto.Monto:N2} x {Gasto.Cotizacion:N2} = ARS {Gasto.MontoEnPesos:N2}";
                }

                return $"ARS {Gasto.MontoEnPesos:N2}";
            }
        }
    }
}