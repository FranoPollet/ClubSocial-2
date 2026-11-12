using System.Net.Http;

namespace ClubSocial.Tests
{
    // Fake: Reemplazará al "mensajero" real de HttpClient; En vez de ir a internet, ejecuta lo que le paso en cada test

    public class HttpMessageHandlerFake : HttpMessageHandler
    {
        private readonly Func<HttpRequestMessage, HttpResponseMessage> _comportamiento;

        public HttpMessageHandlerFake(Func<HttpRequestMessage, HttpResponseMessage> comportamiento)
        {
            _comportamiento = comportamiento;
        }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // Si el comportamiento lanza una excepción, simula un fallo de red
            return Task.FromResult(_comportamiento(request));
        }
    }
}