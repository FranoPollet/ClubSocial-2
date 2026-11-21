using ClubSocial.Models;
using ClubSocial.Repositories;
using ClubSocial.Services;
using Moq;
using Xunit;

namespace ClubSocial.Tests
{
    public class PrecioServiceTests
    {
        // El repositorio es un mock: no hay base de datos, solo probamos la lógica del servicio
        private readonly Mock<IPrecioRepository> _repositorio = new();
        private readonly PrecioService _servicio;

        public PrecioServiceTests()
        {
            _servicio = new PrecioService(_repositorio.Object);
        }

        [Fact]
        public async Task ObtenerPrecioAsync_PideAlRepositorioElPrecioDeEsaFecha()
        {
            // Arrange
            var fecha = new DateTime(2026, 11, 20);
            var precio = new Precio { Tipo = TipoConcepto.Cuota, ConceptoId = 1, Monto = 12000m };

            _repositorio
                .Setup(r => r.ObtenerVigenteAsync(TipoConcepto.Cuota, 1, fecha))
                .ReturnsAsync(precio);

            // Act
            var resultado = await _servicio.ObtenerPrecioAsync(TipoConcepto.Cuota, 1, fecha);

            // Assert
            Assert.NotNull(resultado);
            Assert.Equal(12000m, resultado!.Monto);
            _repositorio.Verify(r => r.ObtenerVigenteAsync(TipoConcepto.Cuota, 1, fecha), Times.Once);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-100)]
        public async Task CambiarPrecioAsync_MontoNoPositivo_DevuelveErrorYNoGuarda(int monto)
        {
            // Act
            var error = await _servicio.CambiarPrecioAsync(
                TipoConcepto.Cuota, 1, monto, Moneda.ARS, new DateTime(2026, 11, 15));

            // Assert: hay error y el repositorio nunca recibió un guardado
            Assert.NotNull(error);
            _repositorio.Verify(r => r.GuardarAsync(It.IsAny<Precio>()), Times.Never);
        }

        [Theory]
        [InlineData(0)]   // el mismo día del último precio
        [InlineData(5)]   // 5 días antes
        public async Task CambiarPrecioAsync_FechaIgualOAnteriorAlUltimoPrecio_DevuelveErrorYNoGuarda(int diasAntes)
        {
            // Arrange: el último precio rige desde el 15/11
            var ultimo = new Precio { VigenteDesde = new DateTime(2026, 11, 15), Monto = 10000m };

            _repositorio
                .Setup(r => r.ObtenerHistorialAsync(TipoConcepto.Cuota, 1))
                .ReturnsAsync(new List<Precio> { ultimo });

            // Act
            var error = await _servicio.CambiarPrecioAsync(
                TipoConcepto.Cuota, 1, 12000m, Moneda.ARS, ultimo.VigenteDesde.AddDays(-diasAntes));

            // Assert
            Assert.NotNull(error);
            _repositorio.Verify(r => r.GuardarAsync(It.IsAny<Precio>()), Times.Never);
        }

        [Fact]
        public async Task CambiarPrecioAsync_PrimerPrecioDelConcepto_LoGuardaSoloConLaFecha()
        {
            // Arrange: todavía no hay precios cargados
            _repositorio
                .Setup(r => r.ObtenerHistorialAsync(TipoConcepto.Cuota, 1))
                .ReturnsAsync(new List<Precio>());

            // Le paso una fecha con hora para comprobar que se guarda solo el día
            var desde = new DateTime(2026, 11, 1, 15, 30, 0);

            // Act
            var error = await _servicio.CambiarPrecioAsync(TipoConcepto.Cuota, 1, 10000m, Moneda.ARS, desde);

            // Assert
            Assert.Null(error);
            _repositorio.Verify(r => r.GuardarAsync(It.Is<Precio>(p =>
                p.Tipo == TipoConcepto.Cuota &&
                p.ConceptoId == 1 &&
                p.Monto == 10000m &&
                p.Moneda == Moneda.ARS &&
                p.VigenteDesde == new DateTime(2026, 11, 1))), Times.Once);
        }

        [Fact]
        public async Task CambiarPrecioAsync_FechaPosteriorAlUltimoPrecio_GuardaElPrecioNuevo()
        {
            // Arrange: el último precio rige desde el 1/10
            var ultimo = new Precio { VigenteDesde = new DateTime(2026, 10, 1), Monto = 10000m };

            _repositorio
                .Setup(r => r.ObtenerHistorialAsync(TipoConcepto.Cuota, 1))
                .ReturnsAsync(new List<Precio> { ultimo });

            // Act
            var error = await _servicio.CambiarPrecioAsync(
                TipoConcepto.Cuota, 1, 12000m, Moneda.ARS, new DateTime(2026, 11, 15));

            // Assert
            Assert.Null(error);
            _repositorio.Verify(r => r.GuardarAsync(It.IsAny<Precio>()), Times.Once);
        }
    }
}