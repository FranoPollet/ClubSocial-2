using ClubSocial.Models;
using ClubSocial.Repositories;
using SQLite;
using Xunit;

namespace ClubSocial.Tests
{
    [Collection("BaseDeDatos")]
    public class PrecioRepositoryTests : IAsyncLifetime
    {
        private SQLiteAsyncConnection _conexion = null!;
        private PrecioRepositorySQLite _repositorio = null!;

        public Task InitializeAsync()
        {
            _conexion = new SQLiteAsyncConnection(":memory:");
            _repositorio = new PrecioRepositorySQLite(_conexion);

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _conexion.CloseAsync();
        }

        // Arma un precio en pesos para no repetir código en cada prueba
        private static Precio NuevoPrecio(TipoConcepto tipo, int conceptoId, decimal monto, DateTime desde)
        {
            return new Precio
            {
                Tipo = tipo,
                ConceptoId = conceptoId,
                Monto = monto,
                Moneda = Moneda.ARS,
                VigenteDesde = desde
            };
        }

        [Fact]
        public async Task ObtenerVigenteAsync_FechaPosteriorAlAumento_DevuelveElPrecioNuevo()
        {
            //Arrange --> la cuota valia 10000 y desde el 15/11 pasa a valer 12000
            await _repositorio.GuardarAsync(NuevoPrecio(TipoConcepto.Cuota, 1, 10000m, new DateTime(2026, 10, 1)));

            await _repositorio.GuardarAsync(NuevoPrecio(TipoConcepto.Cuota, 1, 12000m, new DateTime(2026, 11, 15)));

            // Act
            var precio = await _repositorio.ObtenerVigenteAsync(TipoConcepto.Cuota, 1, new DateTime(2026, 11, 20));

            //Assert
            Assert.NotNull(precio);
            Assert.Equal(12000m, precio!.Monto);
        }

        [Fact]
        public async Task ObtenerVigenteAsync_FechaAnteriorAlAumento_SigueDevolviendoElPrecioViejo()
        {
            // Arrange: mismo aumento del 15/11
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Cuota, 1, 10000m, new DateTime(2026, 10, 1)));
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Cuota, 1, 12000m, new DateTime(2026, 11, 15)));

            // Act: consultamos una fecha anterior al aumento
            var precio = await _repositorio.ObtenerVigenteAsync(
                TipoConcepto.Cuota, 1, new DateTime(2026, 11, 10));

            // Assert: el pasado no cambió
            Assert.NotNull(precio);
            Assert.Equal(10000m, precio!.Monto);
        }

        [Fact]
        public async Task ObtenerVigenteAsync_FechaAnteriorAlPrimerPrecio_DevuelveNull()
        {
            // Arrange
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Cuota, 1, 10000m, new DateTime(2026, 10, 1)));

            // Act
            var precio = await _repositorio.ObtenerVigenteAsync(
                TipoConcepto.Cuota, 1, new DateTime(2026, 9, 1));

            // Assert
            Assert.Null(precio);
        }

        [Fact]
        public async Task ObtenerVigenteAsync_ConceptosDistintos_NoMezclaLosPrecios()
        {
            // Arrange: mismo Id pero distinto tipo, y mismo tipo pero distinto Id
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Cuota, 1, 10000m, new DateTime(2026, 10, 1)));
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Actividad, 1, 5000m, new DateTime(2026, 10, 1)));
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Cuota, 2, 8000m, new DateTime(2026, 10, 1)));

            // Act
            var precio = await _repositorio.ObtenerVigenteAsync(
                TipoConcepto.Actividad, 1, new DateTime(2026, 11, 1));

            // Assert
            Assert.NotNull(precio);
            Assert.Equal(5000m, precio!.Monto);
        }

        [Fact]
        public async Task ObtenerHistorialAsync_VariosPrecios_LosDevuelveDelMasNuevoAlMasViejo()
        {
            // Arrange
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Cuota, 1, 10000m, new DateTime(2026, 10, 1)));
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Cuota, 1, 12000m, new DateTime(2026, 11, 15)));
            await _repositorio.GuardarAsync(
                NuevoPrecio(TipoConcepto.Cuota, 1, 9000m, new DateTime(2026, 8, 1)));

            // Act
            var historial = await _repositorio.ObtenerHistorialAsync(TipoConcepto.Cuota, 1);

            // Assert
            Assert.Equal(3, historial.Count);
            Assert.Equal(12000m, historial[0].Monto);
            Assert.Equal(10000m, historial[1].Monto);
            Assert.Equal(9000m, historial[2].Monto);
        }

        [Fact]
        public async Task GuardarAsync_FechaConHora_GuardaSoloLaFecha()
        {
            // Arrange
            var precio = NuevoPrecio(
                TipoConcepto.Cuota, 1, 10000m, new DateTime(2026, 11, 15, 18, 30, 0));

            // Act
            await _repositorio.GuardarAsync(precio);
            var historial = await _repositorio.ObtenerHistorialAsync(TipoConcepto.Cuota, 1);

            // Assert
            Assert.Equal(new DateTime(2026, 11, 15), historial[0].VigenteDesde);
        }


    }
}