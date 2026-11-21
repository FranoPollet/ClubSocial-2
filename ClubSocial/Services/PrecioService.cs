using System;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;

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