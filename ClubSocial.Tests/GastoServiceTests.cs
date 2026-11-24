using ClubSocial.Models;
using ClubSocial.Repositories;
using ClubSocial.Services;
using Moq;
using Xunit;

namespace ClubSocial.Tests
{
    public class GastoServiceTests
    {
        [Fact]
        public async Task RegistrarGastoAsync_GastoEnDolares_GuardaLaCopiaConLaCotizacionYLosPesos()
        {
            // Arrange: un gasto de 100 dólares con el dólar a 1570
            var repositorio = new Mock<IGastoRepository>();
            var servicio = new GastoService(repositorio.Object);

            // Act
            var error = await servicio.RegistrarGastoAsync(
                "Compra de pelotas", 100m, Moneda.USD, 1570m, new DateTime(2026, 11, 18), 2);

            // Assert
            Assert.Null(error);
            repositorio.Verify(r => r.GuardarAsync(It.Is<Gasto>(g =>
                g.Monto == 100m &&
                g.Moneda == Moneda.USD &&
                g.Cotizacion == 1570m &&
                g.MontoEnPesos == 157000m &&
                g.CuentaId == 2 &&
                g.Periodo == new DateTime(2026, 11, 1))), Times.Once);
        }
    }
}