using System.Net;
using System.Net.Http;
using ClubSocial.Services;
using Xunit;

namespace ClubSocial.Tests
{
    public class ClimaServiceTests
    {
        //Json de ejemplo con 2 días, igual al que devuelve Open-Meteo
        private const string JsonValido =
            "{\"daily\":{" +
            "\"time\":[\"2026-11-12\",\"2026-11-13\"]," +
            "\"temperature_2m_min\":[15.5,16.0]," +
            "\"temperature_2m_max\":[24.0,25.5]," +
            "\"precipitation_probability_max\":[10,60]}}";

        //Json roto para probar el erro de formato
        private const string JsonNoEsJson = "Esto no es un json";

        private const string JsonSinDaily = "{}";

        private const string JsonListasDeDistintoTamanio =
            "{\"daily\":{" +
            "\"time\":[\"2026-11-12\",\"2026-11-13\"]," +
            "\"temperature_2m_min\":[15.5]," +
            "\"temperature_2m_max\":[24.0,25.5]," +
            "\"precipitation_probability_max\":[10,60]}}";

        private const string JsonConFechaInvalida =
            "{\"daily\":{" +
            "\"time\":[\"12/11/2026\"]," +
            "\"temperature_2m_min\":[15.5]," +
            "\"temperature_2m_max\":[24.0]," +
            "\"precipitation_probability_max\":[10]}}";

        // Armo un ClimaService que usa un HttpClient falso
        private static ClimaService CrearServicio(Func<HttpRequestMessage, HttpResponseMessage> comportamiento)
        {
            var handler = new HttpMessageHandlerFake(comportamiento);

            var httpClient = new HttpClient(handler)
            {
                BaseAddress = new Uri("https://api.open-meteo.com/")
            };

            return new ClimaService(httpClient);
        }


        [Fact]
        public async Task ObtenerPronosticoAsync_RespuestaValida_DevuelveLosDias()
        {
            //Arrange
            var servicio = CrearServicio(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(JsonValido)
                });

            //Act
            var dias = await servicio.ObtenerPronosticoAsync();
            //Assert
            Assert.Equal(2, dias.Count);
            Assert.Equal(new DateTime(2026, 11, 12), dias[0].Fecha);
            Assert.Equal(15.5, dias[0].TemperaturaMinima);
            Assert.Equal(25.5, dias[1].TemperaturaMaxima);
            Assert.Equal(60, dias[1].ProbabilidadLluvia);
        }

        [Theory]
        [InlineData(400, TipoErrorClima.SolicitudInvalida)]
        [InlineData(404, TipoErrorClima.NoEncontrado)]
        [InlineData(500, TipoErrorClima.ErrorServidor)]
        [InlineData(503, TipoErrorClima.ErrorHttp)]
        public async Task ObtenerPronosticoAsync_CodigoHttpDeError_LanzaClimaExceptionConSuTipo(
            int codigo,
            TipoErrorClima tipoEsperado)
        {
            // Arrange
            var servicio = CrearServicio(_ =>
                new HttpResponseMessage((HttpStatusCode)codigo));

            // Act
            var ex = await Assert.ThrowsAsync<ClimaException>(
                () => servicio.ObtenerPronosticoAsync());

            // Assert
            Assert.Equal(tipoEsperado, ex.Tipo);
            Assert.Equal(codigo, ex.CodigoHttp);
        }

        [Fact]
        public async Task ObtenerPronosticoAsync_SinConexion_LanzaSinConexionYSinCodigoHttp()
        {
            // Arrange: el handler simula que no hay red
            var servicio = CrearServicio(_ =>
                throw new HttpRequestException("sin red"));

            // Act
            var ex = await Assert.ThrowsAsync<ClimaException>(
                () => servicio.ObtenerPronosticoAsync());

            // Assert
            Assert.Equal(TipoErrorClima.SinConexion, ex.Tipo);
            Assert.Null(ex.CodigoHttp);
        }

        [Fact]
        public async Task ObtenerPronosticoAsync_TimeoutVencido_LanzaTimeout()
        {
            // Arrange: HttpClient lanza TaskCanceledException cuando se vence el timeout
            var servicio = CrearServicio(_ =>
                throw new TaskCanceledException());

            // Act
            var ex = await Assert.ThrowsAsync<ClimaException>(
                () => servicio.ObtenerPronosticoAsync());

            // Assert
            Assert.Equal(TipoErrorClima.Timeout, ex.Tipo);
        }

        [Theory]
        [InlineData(JsonNoEsJson)]
        [InlineData(JsonSinDaily)]
        [InlineData(JsonListasDeDistintoTamanio)]
        [InlineData(JsonConFechaInvalida)]
        public async Task ObtenerPronosticoAsync_JsonInvalido_LanzaFormatoInvalido(
            string json)
        {
            // Arrange
            var servicio = CrearServicio(_ =>
                new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent(json)
                });

            // Act
            var ex = await Assert.ThrowsAsync<ClimaException>(
                () => servicio.ObtenerPronosticoAsync());

            // Assert
            Assert.Equal(TipoErrorClima.FormatoInvalido, ex.Tipo);
        }
    }
}
