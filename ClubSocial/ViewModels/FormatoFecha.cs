using System;
using System.Globalization;

namespace ClubSocial.ViewModels
{
    // Formatos de fecha que comparten varias pantallas
    public static class FormatoFecha
    {
        // Devuelve el mes y el año con la primera letra en mayúscula, por ejemplo "Noviembre 2026"
        public static string MesYAnio(DateTime fecha)
        {
            string texto = fecha.ToString("MMMM yyyy", new CultureInfo("es-AR"));

            return char.ToUpper(texto[0]) + texto.Substring(1);
        }
    }
}