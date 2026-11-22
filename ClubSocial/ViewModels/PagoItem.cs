using ClubSocial.Models;

namespace ClubSocial.ViewModels
{
    // Un pago del mes con los textos ya armados para mostrar en la lista
    public class PagoItem
    {
        public Pago Pago { get; set; } = new Pago();

        public string SocioTexto { get; set; } = string.Empty;

        public string Detalle => Pago.Detalle;

        public string FechaTexto => $"Pagado el {Pago.FechaPago:dd/MM/yyyy}";

        public string MontoTexto
        {
            get
            {
                if (Pago.ConVale)
                {
                    return "Con vale (sin costo)";
                }

                if (Pago.Moneda == Moneda.USD)
                {
                    return $"USD {Pago.Monto:N2} x {Pago.Cotizacion:N2} = ARS {Pago.MontoEnPesos:N2}";
                }

                return $"ARS {Pago.MontoEnPesos:N2}";
            }
        }
    }
}