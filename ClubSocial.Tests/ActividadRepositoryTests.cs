using ClubSocial.Models;
using ClubSocial.Repositories;
using SQLite;
using Xunit;

namespace ClubSocial.Tests
{
    // Misma colección que los otros tests de base de datos, así no corren a la vez
    [Collection("BaseDeDatos")]
    public class ActividadRepositoryTests : IAsyncLifetime
    {
        private SQLiteAsyncConnection _conexion = null!;
        private ActividadRepositorySQLite _repositorio = null!;

        public Task InitializeAsync()
        {
            _conexion = new SQLiteAsyncConnection(":memory:");
            _repositorio = new ActividadRepositorySQLite(_conexion);

            return Task.CompletedTask;
        }

        public async Task DisposeAsync()
        {
            await _conexion.CloseAsync();
        }

        [Fact]
        public async Task GuardarAsync_ActividadNueva_LeAsignaUnId()
        {
            // Arrange
            var actividad = new Actividad { Nombre = "Natación" };

            // Act
            await _repositorio.GuardarAsync(actividad);

            // Assert
            Assert.True(actividad.Id > 0);
        }

        [Fact]
        public async Task ObtenerTodosAsync_VariasActividades_LasDevuelveOrdenadasPorNombre()
        {
            await _repositorio.GuardarAsync(new Actividad { Nombre = "Natación" });
            await _repositorio.GuardarAsync(new Actividad { Nombre = "Básquet" });
            await _repositorio.GuardarAsync(new Actividad { Nombre = "Fútbol" });

            var lista = await _repositorio.ObtenerTodosAsync();

            Assert.Equal(3, lista.Count);
            Assert.Equal("Básquet", lista[0].Nombre);
            Assert.Equal("Natación", lista[2].Nombre);
        }

        [Fact]
        public async Task GuardarAsync_NombreRepetido_LanzaExcepcion()
        {
            await _repositorio.GuardarAsync(new Actividad { Nombre = "Fútbol" });

            // El índice único de la tabla no deja guardar el mismo nombre dos veces
            await Assert.ThrowsAsync<SQLiteException>(
                () => _repositorio.GuardarAsync(new Actividad { Nombre = "Fútbol" }));
        }

        [Fact]
        public async Task EliminarAsync_ActividadGuardada_YaNoApareceEnLaLista()
        {
            var actividad = new Actividad { Nombre = "Tenis" };
            await _repositorio.GuardarAsync(actividad);

            await _repositorio.EliminarAsync(actividad);

            var lista = await _repositorio.ObtenerTodosAsync();
            Assert.Empty(lista);
        }
    }
}