using System;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;

namespace ClubSocial.Services
{
    // Reglas para cobrar cuotas y actividades. Cada método devuelve el mensaje de error, o null si salió bien.
    public class CobranzaService
    {
        private readonly IPagoRepository _repositorio;
        private readonly PrecioService _precioService;

        public CobranzaService(IPagoRepository repositorio, PrecioService precioService)
        {
            _repositorio = repositorio;
            _precioService = precioService;
        }

        // El período de un pago siempre es el primer día del mes
        public static DateTime PrimerDiaDelMes(DateTime fecha)
        {
            return new DateTime(fecha.Year, fecha.Month, 1);
        }

        // El socio tiene vale si su categoría lo incluye y todavía no lo usó en ese mes
        public async Task<bool> ValeDisponibleAsync(int socioId, Categoria? categoria, DateTime periodo)
        {
            if (categoria == null || !categoria.IncluyeVale)
            {
                return false;
            }

            var pagosDelMes = await _repositorio.ObtenerPorPeriodoAsync(PrimerDiaDelMes(periodo));

            return !pagosDelMes.Any(pago => pago.SocioId == socioId && pago.ConVale);
        }

        public async Task<string?> RegistrarCuotaAsync(
            int socioId,
            Categoria categoria,
            DateTime periodo,
            DateTime fechaPago,
            decimal cotizacion,
            int cuentaId = 0)
        {
            periodo = PrimerDiaDelMes(periodo);

            var pagosDelMes = await _repositorio.ObtenerPorPeriodoAsync(periodo);

            // La cuota se paga una sola vez por mes
            if (pagosDelMes.Any(pago => pago.SocioId == socioId && pago.Tipo == TipoConcepto.Cuota))
            {
                return "Ese socio ya pagó la cuota de ese mes.";
            }

            // Se cobra el precio que estaba vigente el día del pago
            var precio = await _precioService.ObtenerPrecioAsync(TipoConcepto.Cuota, categoria.Id, fechaPago);

            return await GuardarCobroAsync(
                socioId, TipoConcepto.Cuota, categoria.Id, $"Cuota {categoria.Nombre}",
                precio, periodo, fechaPago, cotizacion, cuentaId);
        }

        public async Task<string?> RegistrarActividadAsync(
            int socioId,
            Categoria? categoria,
            Actividad actividad,
            DateTime periodo,
            DateTime fechaPago,
            decimal cotizacion,
            bool usarVale,
            int cuentaId = 0)
        {
            periodo = PrimerDiaDelMes(periodo);

            if (usarVale)
            {
                if (!await ValeDisponibleAsync(socioId, categoria, periodo))
                {
                    return "El socio no tiene un vale disponible en ese mes.";
                }

                // Con vale no se cobra nada: el monto queda en 0 y no entra a ninguna cuenta
                var pagoConVale = new Pago
                {
                    SocioId = socioId,
                    Tipo = TipoConcepto.Actividad,
                    ConceptoId = actividad.Id,
                    Periodo = periodo,
                    FechaPago = fechaPago.Date,
                    Monto = 0,
                    Moneda = Moneda.ARS,
                    Cotizacion = 0,
                    MontoEnPesos = 0,
                    ConVale = true,
                    Detalle = $"Actividad {actividad.Nombre} (vale)"
                };

                await _repositorio.GuardarAsync(pagoConVale);

                return null;
            }

            var precio = await _precioService.ObtenerPrecioAsync(TipoConcepto.Actividad, actividad.Id, fechaPago);

            return await GuardarCobroAsync(
                socioId, TipoConcepto.Actividad, actividad.Id, $"Actividad {actividad.Nombre}",
                precio, periodo, fechaPago, cotizacion, cuentaId);
        }

        // Arma el pago con una copia de los valores de ese momento y lo guarda
        private async Task<string?> GuardarCobroAsync(
            int socioId,
            TipoConcepto tipo,
            int conceptoId,
            string detalle,
            Precio? precio,
            DateTime periodo,
            DateTime fechaPago,
            decimal cotizacion,
            int cuentaId)
        {
            if (precio == null)
            {
                return "No hay un precio cargado para esa fecha.";
            }

            bool enDolares = precio.Moneda == Moneda.USD;

            if (enDolares && cotizacion <= 0)
            {
                return "Ingresá la cotización del dólar.";
            }

            var pago = new Pago
            {
                SocioId = socioId,
                Tipo = tipo,
                ConceptoId = conceptoId,
                Periodo = periodo,
                FechaPago = fechaPago.Date,
                Monto = precio.Monto,
                Moneda = precio.Moneda,
                Cotizacion = enDolares ? cotizacion : 0,
                MontoEnPesos = enDolares ? Math.Round(precio.Monto * cotizacion, 2) : precio.Monto,
                ConVale = false,
                CuentaId = cuentaId,
                Detalle = detalle
            };

            await _repositorio.GuardarAsync(pago);

            return null;
        }
    }
}