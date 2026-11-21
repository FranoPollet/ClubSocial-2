using System;
using System.Collections.Generic;
using System.Linq;

namespace ClubSocial.Services
{
    // Reglas para crear una categoría. Igual que ValidadorSocio, las saco del ViewModel.
    public static class ValidadorCategoria
    {
        // Devuelve el mensaje de error, o null si el nombre está bien
        public static string? Validar(string? nombre, IEnumerable<string> nombresExistentes)
        {
            if (string.IsNullOrWhiteSpace(nombre))
            {
                return "Escribí el nombre de la categoría.";
            }

            string limpio = nombre.Trim();

            // Comparo sin importar mayúsculas: "cadete" y "Cadete" son la misma categoría
            bool repetido = nombresExistentes.Any(existente =>
                string.Equals(existente, limpio, StringComparison.OrdinalIgnoreCase));

            if (repetido)
            {
                return "Ya existe una categoría con ese nombre.";
            }

            return null;
        }
    }
}