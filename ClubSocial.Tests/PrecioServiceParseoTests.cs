using ClubSocial.Services;
using Xunit;

namespace ClubSocial.Tests
{
    public class PrecioServiceParseoTests
    {
        [Theory]
        [InlineData("12000", 12000)]
        [InlineData("12000,50", 12000.5)]
        public void TryParsearMonto_TextoValido_DevuelveElMonto(string texto, double esperado)
        {
            bool ok = PrecioService.TryParsearMonto(texto, out decimal monto);

            Assert.True(ok);
            // [InlineData] no acepta decimal, por eso el esperado viene como double
            Assert.Equal((decimal)esperado, monto);
        }

        [Theory]
        [InlineData("12.000")]   // el punto no se acepta
        [InlineData("abc")]
        [InlineData("")]
        [InlineData("-5")]
        public void TryParsearMonto_TextoInvalido_DevuelveFalse(string texto)
        {
            bool ok = PrecioService.TryParsearMonto(texto, out _);

            Assert.False(ok);
        }
    }
}