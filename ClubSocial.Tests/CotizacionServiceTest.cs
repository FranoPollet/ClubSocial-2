using System.Net;
using System.Net.Http;
using ClubSocial.Services;
using Xunit;

namespace ClubSocial.Tests
{
    public class CotizacionServiceTests
    {
        // Respuesta real de DolarApi (el valor que usamos es "venta")
        private const string JsonDolar =
            "{\"moneda\":\"USD\",\"casa\":\"blue\",\"nombre\":\"Blue\"," +
            "\"compra\":1550,\"venta\":1570,\"fechaActualizacion\":\"2026-11-11T20:56:00.000Z\"}";

        // Armo un servicio que usa un HttpClient falso
        private static CotizacionService CrearServicio(Func<HttpRequestMessage, HttpResponseMessage> comportamiento)
        {
            var handler = new HttpMessageHandlerFake(comportamiento);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://dolarapi.com/")
            };

            return new CotizacionService(httpClient);
        }

        [Fact]
        public async Task ObtenerDolarBlueAsync_RespuestaValida_DevuelveElValorDeVenta()
        {
            // Arrange
            var servicio = CrearServicio(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonDolar)
                });

            // Act
            decimal valor = await servicio.ObtenerDolarBlueAsync();

            // Assert: toma venta (1570) y no compra (1550)
            Assert.Equal(1570m, valor);
        }

        [Fact]
        public async Task ObtenerDolarBlueAsync_ErrorDelServidor_LanzaCotizacionException()
        {
            var servicio = CrearServicio(_ => new HttpResponseMessage(HttpStatusCode.InternalServerError));

            await Assert.ThrowsAsync<CotizacionException>(() => servicio.ObtenerDolarBlueAsync());
        }
    }
}