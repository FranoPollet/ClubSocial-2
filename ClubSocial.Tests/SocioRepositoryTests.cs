using ClubSocial.Models;
using ClubSocial.Repositories;
using SQLite;
using Xunit;

namespace ClubSocial.Tests
{
    [Collection("BaseDeDatos")]
    public class SocioRepositoryTests : IAsyncLifetime
    {
        private SQLiteAsyncConnection _conexion = null!;
        private SocioRepositorySQLite _repositorio = null!;

        public Task InitializeAsync()
        {
            _conexion = new SQLiteAsyncConnection(":memory:");
            _repositorio = new SocioRepositorySQLite(_conexion);

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _conexion.CloseAsync();
        }

        // Socio de prueba con datos inventados.
        private static Socio NuevoSocio(string dni)
        {
            return new Socio
            {
                Nombre = "Enzo",
                Apellido = "Alvarez",
                Dni = dni,
                Telefono = "1120221218",
                Sector = "Social"
            };
        }

        [Fact]
        public async Task GuardarAsync_SocioNuevo_LoInsertaYLeAsignaUnId()
        {
            // Arrange
            var socio = NuevoSocio("38120918");

            // Act
            await _repositorio.GuardarAsync(socio);

            // Assert
            Assert.True(socio.Id > 0);
        }

        [Fact]
        public async Task ObtenerPorDniAsync_DniExistente_DevuelveElSocio()
        {
            // Arrange
            await _repositorio.GuardarAsync(NuevoSocio("38120918"));

            // Act
            var socio = await _repositorio.ObtenerPorDniAsync("38120918");

            // Assert
            Assert.NotNull(socio);
            Assert.Equal("Enzo", socio!.Nombre);
        }

        [Fact]
        public async Task ObtenerPorDniAsync_DniInexistente_DevuelveNull()
        {
            // Act
            var socio = await _repositorio.ObtenerPorDniAsync("99999999");

            // Assert
            Assert.Null(socio);
        }

        [Fact]
        public async Task GuardarAsync_DniRepetido_LanzaExcepcion()
        {
            // Arrange
            await _repositorio.GuardarAsync(NuevoSocio("38120918"));

            // Act y Assert: el índice único de Dni no permite el duplicado
            await Assert.ThrowsAnyAsync<SQLiteException>(
                () => _repositorio.GuardarAsync(NuevoSocio("38120918")));
        }
    }
}