using System;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;

namespace ClubSocial.Services
{
    // Reglas para registrar un gasto. Devuelve el mensaje de error, o null si se guardó bien.
    public class GastoService
    {
        private readonly IGastoRepository _repositorio;

        public GastoService(IGastoRepository repositorio)
        {
            _repositorio = repositorio;
        }

        public async Task<string?> RegistrarGastoAsync(
            string? descripcion,
            decimal monto,
            Moneda moneda,
            decimal cotizacion,
            DateTime fecha,
            int cuentaId)
        {
            if (string.IsNullOrWhiteSpace(descripcion))
            {
                return "Escribí en qué se gastó.";
            }

            if (monto <= 0)
            {
                return "El monto tiene que ser mayor a cero.";
            }

            if (cuentaId <= 0)
            {
                return "Elegí la cuenta de la que salió la plata.";
            }

            bool enDolares = moneda == Moneda.USD;

            if (enDolares && cotizacion <= 0)
            {
                return "Ingresá la cotización del dólar.";
            }

            // Guardo una copia de los valores del momento, igual que en los pagos
            var gasto = new Gasto
            {
                Fecha = fecha.Date,
                Periodo = CobranzaService.PrimerDiaDelMes(fecha),
                Descripcion = descripcion.Trim(),
                Monto = monto,
                Moneda = moneda,
                Cotizacion = enDolares ? cotizacion : 0,
                MontoEnPesos = enDolares ? Math.Round(monto * cotizacion, 2) : monto,
                CuentaId = cuentaId
            };

            await _repositorio.GuardarAsync(gasto);

            return null;
        }
    }
}