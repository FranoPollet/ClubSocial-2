using ClubSocial.Models;
using ClubSocial.Repositories;
using ClubSocial.Services;
using Moq;
using Xunit;

namespace ClubSocial.Tests
{
    public class CobranzaServiceTests
    {
        private readonly Mock<IPagoRepository> _pagos = new();
        private readonly Mock<IPrecioRepository> _precios = new();
        private readonly CobranzaService _servicio;

        private static readonly Categoria Cadete = new Categoria { Id = 1, Nombre = "Cadete", IncluyeVale = true };

        public CobranzaServiceTests()
        {
            // PrecioService es una clase, no una interfaz: uso la real con un repositorio de precios falso
            _servicio = new CobranzaService(_pagos.Object, new PrecioService(_precios.Object));
        }

        [Fact]
        public async Task RegistrarCuotaAsync_PrecioEnDolares_GuardaLaCopiaConLaCotizacionYLosPesos()
        {
            // Arrange: la cuota vale 20 dólares y el dólar está a 1570
            _precios
                .Setup(r => r.ObtenerVigenteAsync(TipoConcepto.Cuota, 1, It.IsAny<DateTime>()))
                .ReturnsAsync(new Precio { Monto = 20m, Moneda = Moneda.USD });

            _pagos
                .Setup(r => r.ObtenerPorPeriodoAsync(new DateTime(2026, 11, 1)))
                .ReturnsAsync(new List<Pago>());

            // Act: le paso una fecha a mitad de mes para comprobar que el período queda en el día 1
            var error = await _servicio.RegistrarCuotaAsync(
                5, Cadete, new DateTime(2026, 11, 20), new DateTime(2026, 11, 21), 1570m);

            // Assert
            Assert.Null(error);
            _pagos.Verify(r => r.GuardarAsync(It.Is<Pago>(p =>
                p.SocioId == 5 &&
                p.Tipo == TipoConcepto.Cuota &&
                p.Periodo == new DateTime(2026, 11, 1) &&
                p.Monto == 20m &&
                p.Moneda == Moneda.USD &&
                p.Cotizacion == 1570m &&
                p.MontoEnPesos == 31400m)), Times.Once);
        }

        [Fact]
        public async Task RegistrarCuotaAsync_YaPagoEseMes_DevuelveErrorYNoGuarda()
        {
            // Arrange: el socio 5 ya tiene la cuota de noviembre
            _pagos
                .Setup(r => r.ObtenerPorPeriodoAsync(new DateTime(2026, 11, 1)))
                .ReturnsAsync(new List<Pago>
                {
                    new Pago { SocioId = 5, Tipo = TipoConcepto.Cuota }
                });

            // Act
            var error = await _servicio.RegistrarCuotaAsync(
                5, Cadete, new DateTime(2026, 11, 1), new DateTime(2026, 11, 21), 0m);

            // Assert
            Assert.NotNull(error);
            _pagos.Verify(r => r.GuardarAsync(It.IsAny<Pago>()), Times.Never);
        }

        [Fact]
        public async Task RegistrarActividadAsync_ValeYaUsadoEnElMes_DevuelveErrorYNoGuarda()
        {
            // Arrange: el socio 5 ya usó su vale en noviembre
            _pagos
                .Setup(r => r.ObtenerPorPeriodoAsync(new DateTime(2026, 11, 1)))
                .ReturnsAsync(new List<Pago>
                {
                    new Pago { SocioId = 5, Tipo = TipoConcepto.Actividad, ConVale = true }
                });

            var natacion = new Actividad { Id = 2, Nombre = "Natación" };

            // Act
            var error = await _servicio.RegistrarActividadAsync(
                5, Cadete, natacion, new DateTime(2026, 11, 1), new DateTime(2026, 11, 21), 0m, usarVale: true);

            // Assert
            Assert.NotNull(error);
            _pagos.Verify(r => r.GuardarAsync(It.IsAny<Pago>()), Times.Never);
        }
    }
}