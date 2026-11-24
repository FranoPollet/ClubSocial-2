using ClubSocial.Models;
using ClubSocial.Repositories;
using ClubSocial.Services;
using Moq;
using Xunit;

namespace ClubSocial.Tests
{
    public class ResumenServiceTests
    {
        [Fact]
        public async Task ObtenerResumenAsync_PagosYGastos_CalculaElBalanceTotalYPorCuenta()
        {
            // Arrange: noviembre con dos cobros, un gasto y dos cuentas
            var periodo = new DateTime(2026, 11, 1);

            var pagos = new Mock<IPagoRepository>();
            pagos.Setup(r => r.ObtenerPorPeriodoAsync(periodo)).ReturnsAsync(new List<Pago>
            {
                new Pago { CuentaId = 1, MontoEnPesos = 10000m },
                new Pago { CuentaId = 2, MontoEnPesos = 5000m }
            });

            var gastos = new Mock<IGastoRepository>();
            gastos.Setup(r => r.ObtenerPorPeriodoAsync(periodo)).ReturnsAsync(new List<Gasto>
            {
                new Gasto { CuentaId = 1, MontoEnPesos = 3000m }
            });

            var cuentas = new Mock<ICuentaRepository>();
            cuentas.Setup(r => r.ObtenerTodosAsync()).ReturnsAsync(new List<Cuenta>
            {
                new Cuenta { Id = 1, Nombre = "Efectivo" },
                new Cuenta { Id = 2, Nombre = "Banco" }
            });

            var servicio = new ResumenService(pagos.Object, gastos.Object, cuentas.Object);

            // Act: le paso una fecha a mitad de mes
            var resumen = await servicio.ObtenerResumenAsync(new DateTime(2026, 11, 20));

            // Assert
            Assert.Equal(15000m, resumen.Ingresos);
            Assert.Equal(3000m, resumen.Gastos);
            Assert.Equal(12000m, resumen.Balance);
            Assert.Equal(7000m, resumen.PorCuenta.Single(c => c.Nombre == "Efectivo").Saldo);
        }
    }
}