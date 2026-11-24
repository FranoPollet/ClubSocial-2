using System;
using System.Collections.Generic;

namespace ClubSocial.Models
{
    // Lo que entró y salió de una cuenta en el mes (en pesos)
    public class ResumenCuenta
    {
        public string Nombre { get; set; } = string.Empty;

        public decimal Ingresos { get; set; }

        public decimal Gastos { get; set; }

        public decimal Saldo => Ingresos - Gastos;
    }

    // Resumen de un mes del club (en pesos)
    public class ResumenMes
    {
        public DateTime Periodo { get; set; }

        public decimal Ingresos { get; set; }

        public decimal Gastos { get; set; }

        public decimal Balance => Ingresos - Gastos;

        public List<ResumenCuenta> PorCuenta { get; set; } = new();
    }
}