using System;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;

namespace ClubSocial.Services
{
    // Arma el resumen de un mes: ingresos (pagos) menos gastos, en total y por cuenta
    public class ResumenService
    {
        private readonly IPagoRepository _pagoRepositorio;
        private readonly IGastoRepository _gastoRepositorio;
        private readonly ICuentaRepository _cuentaRepositorio;

        public ResumenService(
            IPagoRepository pagoRepositorio,
            IGastoRepository gastoRepositorio,
            ICuentaRepository cuentaRepositorio)
        {
            _pagoRepositorio = pagoRepositorio;
            _gastoRepositorio = gastoRepositorio;
            _cuentaRepositorio = cuentaRepositorio;
        }

        public async Task<ResumenMes> ObtenerResumenAsync(DateTime periodo)
        {
            periodo = CobranzaService.PrimerDiaDelMes(periodo);

            var pagos = await _pagoRepositorio.ObtenerPorPeriodoAsync(periodo);
            var gastos = await _gastoRepositorio.ObtenerPorPeriodoAsync(periodo);
            var cuentas = await _cuentaRepositorio.ObtenerTodosAsync();

            // Los importes en pesos ya están guardados en cada pago y cada gasto
            var resumen = new ResumenMes
            {
                Periodo = periodo,
                Ingresos = pagos.Sum(pago => pago.MontoEnPesos),
                Gastos = gastos.Sum(gasto => gasto.MontoEnPesos)
            };

            foreach (var cuenta in cuentas)
            {
                resumen.PorCuenta.Add(new ResumenCuenta
                {
                    Nombre = cuenta.Nombre,
                    Ingresos = pagos.Where(pago => pago.CuentaId == cuenta.Id).Sum(pago => pago.MontoEnPesos),
                    Gastos = gastos.Where(gasto => gasto.CuentaId == cuenta.Id).Sum(gasto => gasto.MontoEnPesos)
                });
            }

            // Movimientos sin cuenta asignada (o de una cuenta que se eliminó)
            var idsConocidos = cuentas.Select(cuenta => cuenta.Id).ToList();

            decimal ingresosSinCuenta = pagos
                .Where(pago => !idsConocidos.Contains(pago.CuentaId))
                .Sum(pago => pago.MontoEnPesos);

            decimal gastosSinCuenta = gastos
                .Where(gasto => !idsConocidos.Contains(gasto.CuentaId))
                .Sum(gasto => gasto.MontoEnPesos);

            if (ingresosSinCuenta != 0 || gastosSinCuenta != 0)
            {
                resumen.PorCuenta.Add(new ResumenCuenta
                {
                    Nombre = "Sin cuenta",
                    Ingresos = ingresosSinCuenta,
                    Gastos = gastosSinCuenta
                });
            }

            return resumen;
        }
    }
}