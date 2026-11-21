using System;
using System.Collections.Generic;
using System.Linq;

namespace ClubSocial.Services
{
    // Reglas para crear una categoría o una actividad (las dos tienen un nombre que no se puede repetir)
    public static class ValidadorCategoria
    {
        // "tipo" es lo que se está validando: "categoría" o "actividad" (las dos son femeninas)
        // Devuelve el mensaje de error, o null si el nombre está bien
        public static string? Validar(
            string? nombre,
            IEnumerable<string> nombresExistentes,
            string tipo = "categoría")
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return $"Escribí el nombre de la {tipo}.";
            }

            string limpio = nombre.Trim();

            // Comparo sin importar mayúsculas: "cadete" y "Cadete" son lo mismo
            bool repetido = nombresExistentes.Any(existente =>
                string.Equals(existente, limpio, StringComparison.OrdinalIgnoreCase));

            if (repetido)
            {
                return $"Ya existe una {tipo} con ese nombre.";
            }

            return null;
        }
    }
}