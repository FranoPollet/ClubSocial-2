using ClubSocial.Services;
using Xunit;

namespace ClubSocial.Tests
{
    public class ValidadorCategoriaTests
    {
        private static readonly string[] Existentes = { "Cadete", "Adulto" };

        [Fact]
        public void Validar_NombreNuevo_DevuelveNull()
        {
            var error = ValidadorCategoria.Validar("Juvenil", Existentes);

            Assert.Null(error);
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void Validar_NombreVacio_DevuelveError(string nombre)
        {
            var error = ValidadorCategoria.Validar(nombre, Existentes);

            Assert.NotNull(error);
        }

        [Theory]
        [InlineData("Cadete")]
        [InlineData("  cadete ")]   // distinta mayúscula y con espacios
        public void Validar_NombreRepetido_DevuelveError(string nombre)
        {
            var error = ValidadorCategoria.Validar(nombre, Existentes);

            Assert.NotNull(error);
        }
    }
}