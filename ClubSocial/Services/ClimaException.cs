using ClubSocial.Services;
using System;

namespace ClubSocial.Services
{
    // Tipos de errores que puede informar la API de Clima OPEN-METEO
    public enum TipoErrorClima
    {
        SinConexion,            // No hay conexión a internet
        Timeout,                // La consulta tardó demasiado tiempo
        SolicitudInvalida,      // HTTP 400
        NoEncontrado,           // HTTP 404
        ErrorServidor,          // HTTP 500
        ErrorHttp,              // Otros errores HTTP
        FormatoInvalido         // El JSON no tiene lo que esperamos
    }


    // Excepción propia --> El servicio ya clasifica el error y el ViewModel solo tiene que elegir qué mensaje mostrar
    public class ClimaException : Exception
    {
        public TipoErrorClima Tipo { get; }

        public int? CodigoHttp { get; }

        public ClimaException(TipoErrorClima tipo, string mensaje, int? codigoHttp = null, Exception? excepcionInterna = null) : base(mensaje, excepcionInterna)
        {
            Tipo = tipo;
            CodigoHttp = codigoHttp;
        }
    }

}