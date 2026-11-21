using System;
using System.Linq;

namespace ClubSocial.Services
{
    //Reglas para validar los datos de un socio, las elimino de socioviewmodel para testearla

    public static class ValidadorSocio
    {
        // Devuelve el mensaje de error, o null si los datos están bien
        public static string? Validar(string? nombre, string? apellido, string? dni)
        {
            if (string.IsNullOrWhiteSpace(nombre) ||
                string.IsNullOrWhiteSpace(apellido))
            {
                return "Completá el nombre y el apellido.";
            }

            string dniLimpio = (dni ?? string.Empty).Trim();

            //  validación de formato
            if (dniLimpio.Length < 7 || dniLimpio.Length > 8 ||
                !dniLimpio.All(caracter => caracter >= '0' && caracter <= '9'))
            {
                return "Ingresá un DNI de 7 u 8 números, sin puntos.";
            }

            return null;
        }

        // El socio tiene que tener una categoría elegida(Id 0 significa "ninguna")
        public static string? ValidarCategoria(int categoriaId)
        {
            if (categoriaId <= 0)
            {
                return "Seleccioná una categoría del club.";
            }

            return null;
        }
    }
}