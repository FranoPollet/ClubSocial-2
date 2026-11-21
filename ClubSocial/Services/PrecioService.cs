using System;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;
using System.Collections.Generic;
using System.Globalization;

namespace ClubSocial.Services
{
    // Reglas de negocio de los precios. El repositorio solo guarda y lee.
    public class PrecioService
    {
        private readonly IPrecioRepository _repositorio;

        // Recibe la interfaz, así en los tests le puedo pasar un mock
        public PrecioService(IPrecioRepository repositorio)
        {
            _repositorio = repositorio;
        }

        // Precio que regía en una fecha (null si todavía no había ninguno cargado)
        public async Task<Precio?> ObtenerPrecioAsync(TipoConcepto tipo, int conceptoId, DateTime fecha)
        {
            return await _repositorio.ObtenerVigenteAsync(tipo, conceptoId, fecha);
        }

        // Todos los precios de un concepto, del más nuevo al más viejo (para mostrar el historial)
        public async Task<List<Precio>> ObtenerHistorialAsync(TipoConcepto tipo, int conceptoId)
        {
            return await _repositorio.ObtenerHistorialAsync(tipo, conceptoId);
        }

        // Convierte el monto que escribió el usuario. Los decimales van con coma.
        public static bool TryParsearMonto(string? texto, out decimal monto)
        {
            monto = 0;

            if (string.IsNullOrWhiteSpace(texto))
            {
                return false;
            }

            string limpio = texto.Trim();

            // No acepto el punto para no confundir 12.000 (doce mil) con 12
            if (limpio.Contains('.'))
            {
                return false;
            }

            limpio = limpio.Replace(',', '.');

            return decimal.TryParse(
                limpio,
                NumberStyles.AllowDecimalPoint,
                CultureInfo.InvariantCulture,
                out monto);
        }

        // Agrega un precio nuevo que rige desde una fecha.
        // Devuelve el mensaje de error, o null si se guardó bien.
        public async Task<string?> CambiarPrecioAsync(
            TipoConcepto tipo,
            int conceptoId,
            decimal monto,
            Moneda moneda,
            DateTime desde)
        {
            if (monto <= 0)
            {
                return "El monto tiene que ser mayor a cero.";
            }

            // El historial viene del más nuevo al más viejo, así que el primero es el último precio cargado
            var historial = await _repositorio.ObtenerHistorialAsync(tipo, conceptoId);
            var ultimo = historial.FirstOrDefault();

            // No dejo cargar una fecha igual o anterior porque cambiaría los precios del pasado
            if (ultimo != null && desde.Date <= ultimo.VigenteDesde.Date)
            {
                return $"Ya hay un precio desde el {ultimo.VigenteDesde:dd/MM/yyyy}. Elegí una fecha posterior.";
            }

            var precioNuevo = new Precio
            {
                Tipo = tipo,
                ConceptoId = conceptoId,
                Monto = monto,
                Moneda = moneda,
                VigenteDesde = desde.Date
            };

            await _repositorio.GuardarAsync(precioNuevo);

            return null;
        }
    }
}