using ClubSocial.Services;
using Xunit;

namespace ClubSocial.Tests
{

    public class ValidadorSocioTests
    {
        [Theory]
        [InlineData("Juan", "Pérez", "1234567")]    // 7 números
        [InlineData("Juan", "Pérez", "12345678")]   // 8 números
        public void Validar_DatosCorrectos_DevuelveNull(string nombre, string apellido, string dni)
        {
            // Act
            var error = ValidadorSocio.Validar(nombre, apellido, dni);

            // Assert
            Assert.Null(error);
        }

        [Theory]
        [InlineData("", "Pérez", "12345678")]
        [InlineData("Juan", "", "12345678")]
        [InlineData("   ", "Pérez", "12345678")]
        [InlineData(null, "Pérez", "12345678")]
        public void Validar_NombreOApellidoVacio_DevuelveError(string? nombre, string? apellido, string dni)
        {
            var error = ValidadorSocio.Validar(nombre, apellido, dni);

            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("123456")]      // muy corto
        [InlineData("123456789")]   // muy largo
        [InlineData("1234567a")]    // tiene una letra
        [InlineData("12.345.67")]   // tiene puntos
        [InlineData("")]
        [InlineData("   ")]
        public void Validar_DniConFormatoIncorrecto_DevuelveError(string dni)
        {
            var error = ValidadorSocio.Validar("Juan", "Pérez", dni);

            Assert.NotNull(error);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(-1)]
        public void ValidarCategoria_SinCategoria_DevuelveError(int categoriaId)
        {
            var error = ValidadorSocio.ValidarCategoria(categoriaId);

            Assert.NotNull(error);
        }

        [Fact]
        public void ValidarCategoria_CategoriaElegida_DevuelveNull()
        {
            var error = ValidadorSocio.ValidarCategoria(3);

            Assert.Null(error);
        }
    }
}