using ClubSocial.Models;
using ClubSocial.Repositories;
using SQLite;
using Xunit;

namespace ClubSocial.Tests
{
    // Todas las pruebas q usan la base van en la misma coleccion así xUnit no las ejecuta en paralelo
    [Collection("BaseDeDatos")]
    public class CategoriaRepositoryTests : IAsyncLifetime
    {
        private SQLiteAsyncConnection _conexion = null!;
        private CategoriaRepositorySQLite _repositorio = null!;

        // xUnit lo ejecuta antes de cada prueba
        public Task InitializeAsync()
        {
            _conexion = new SQLiteAsyncConnection(":memory:");
            _repositorio = new CategoriaRepositorySQLite(_conexion);

            return Task.CompletedTask;
        }

        // xUnit lo ejecuta despues de cada prueba; Cierra la conexión, la base en memoria desaparece y la proxima prueba arranca limpia
        public async Task DisposeAsync()
        {
            await _conexion.CloseAsync();
        }

        [Fact]
        public async Task GuardarAsync_CategoriaNueva_LeAsignaUnId()
        {
            //Arrange
            var categoria = new Categoria { Nombre = "Adulto", IncluyeVale = true };

            //Act
            await _repositorio.GuardarAsync(categoria);

            //Assert
            Assert.True(categoria.Id > 0);
        }

        [Fact]
        public async Task ObtenerTodosAsync_VariasCategorias_LasDevuelveOrdenadasPorNombre()
        {
            // Arrange
            await _repositorio.GuardarAsync(new Categoria { Nombre = "Infantil" });
            await _repositorio.GuardarAsync(new Categoria { Nombre = "Adulto" });
            await _repositorio.GuardarAsync(new Categoria { Nombre = "Cadete" });

            // Act
            var categorias = await _repositorio.ObtenerTodosAsync();

            // Assert
            Assert.Equal(3, categorias.Count);
            Assert.Equal("Adulto", categorias[0].Nombre);
            Assert.Equal("Cadete", categorias[1].Nombre);
            Assert.Equal("Infantil", categorias[2].Nombre);
        }

        [Fact]
        public async Task GuardarAsync_NombreRepetido_LanzaExcepcion()
        {
            // Arrange
            await _repositorio.GuardarAsync(new Categoria { Nombre = "Adulto" });

            // Act y Assert: el índice único de Nombre no permite el duplicado
            await Assert.ThrowsAnyAsync<SQLiteException>(
                () => _repositorio.GuardarAsync(new Categoria { Nombre = "Adulto" }));
        }

        [Fact]
        public async Task EliminarAsync_CategoriaGuardada_LaQuitaDeLaLista()
        {
            // Arrange
            var categoria = new Categoria { Nombre = "Adulto" };
            await _repositorio.GuardarAsync(categoria);

            // Act
            await _repositorio.EliminarAsync(categoria);
            var categorias = await _repositorio.ObtenerTodosAsync();

            // Assert
            Assert.Empty(categorias);
        }
    }
}