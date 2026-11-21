using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Services
{
    public class CotizacionService : ICotizacionService
    {
        private readonly HttpClient _httpClient;

        // Constructor que usa la app; crea el cliente real de DolarApi
        public CotizacionService() : this(CrearHttpClient())
        { }

        // Constructor que recibe el HttpClient, para pasarle uno falso en los tests
        public CotizacionService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private static HttpClient CrearHttpClient()
        {
            return new HttpClient
            {
                BaseAddress = new Uri("https://dolarapi.com/"),
                Timeout = TimeSpan.FromSeconds(15)
            };
        }

        public async Task<decimal> ObtenerDolarBlueAsync()
        {
            try
            {
                using (var respuesta = await _httpClient.GetAsync("v1/dolares/blue"))
                {
                    if (!respuesta.IsSuccessStatusCode)
                    {
                        throw new CotizacionException(
                            $"El servicio respondió con el código HTTP {(int)respuesta.StatusCode}");
                    }

                    string json = await respuesta.Content.ReadAsStringAsync();

                    var dolar = JsonSerializer.Deserialize<RespuestaDolar>(json);

                    if (dolar?.Venta == null || dolar.Venta <= 0)
                    {
                        throw new CotizacionException("La respuesta no trae el valor de venta");
                    }

                    return dolar.Venta.Value;
                }
            }
            catch (CotizacionException)
            {
                // Ya es mi excepción, la dejo pasar tal cual
                throw;
            }
            catch (OperationCanceledException ex)
            {
                // Se venció el timeout
                throw new CotizacionException("La consulta demoró demasiado", ex);
            }
            catch (HttpRequestException ex)
            {
                // Sin internet
                throw new CotizacionException("No se pudo conectar con el servicio del dólar", ex);
            }
            catch (JsonException ex)
            {
                throw new CotizacionException("La respuesta del servicio no tiene el formato esperado", ex);
            }
        }
    }
}