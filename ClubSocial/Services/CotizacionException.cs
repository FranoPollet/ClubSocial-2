using System;

namespace ClubSocial.Services
{
    // Se lanza cuando no se pudo obtener el valor del dólar
    public class CotizacionException : Exception
    {
        public CotizacionException(string mensaje, Exception? excepcionInterna = null)
            : base(mensaje, excepcionInterna)
        { }
    }
}