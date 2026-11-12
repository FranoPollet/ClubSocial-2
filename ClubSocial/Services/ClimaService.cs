using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Services
{
    public class ClimaService
    {
        private readonly HttpClient _httpClient;

        // Constructor que usa la app; crea el cliente real de Open-Meteo
        public ClimaService() : this(CrearHttpClient())
        { }


        // Constructor que recibe el HttpClient desde afuera y lo usamos en los tests para pasarle un cliente con respuestas falsas
        public ClimaService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        private static HttpClient CrearHttpClient()
        {
            return new HttpClient
            {
                BaseAddress = new Uri("https://api.open-meteo.com/"),
                Timeout = TimeSpan.FromSeconds(20)
            };
        }

        public async Task<List<PronosticoDia>> ObtenerPronosticoAsync()
        {
            // BsAs es la ubicación de ejemplo
            string consulta =
                "v1/forecast?latitude=-34.6037&longitude=-58.3816" +
                "&daily=temperature_2m_min,temperature_2m_max," +
                "precipitation_probability_max" +
                "&temperature_unit=celsius" +
                "&timezone=America%2FArgentina%2FBuenos_Aires" +
                "&forecast_days=7";

            HttpResponseMessage respuesta;

            try
            {
                respuesta = await _httpClient.GetAsync(consulta);
            }
            catch (OperationCanceledException ex)
            {
                // HttpClient lanza esta excepcion cuando se vence el timeout
                throw new ClimaException(TipoErrorClima.Timeout, "La consulta demoró demasiado", excepcionInterna: ex);
            }
            catch (HttpRequestException ex)
            {
                // Sin internet 
                throw new ClimaException(TipoErrorClima.SinConexion, "No se pudo conectar con el servicio del clima", excepcionInterna: ex);
            }

            using (respuesta)
            {
                // Si el servidor respondió con algún codigo de error lo clasificamos (corrección del parcial 1 que me mencionó el Profesor Nicolas)
                if (!respuesta.IsSuccessStatusCode)
                {
                    throw CrearErrorHttp(respuesta.StatusCode);
                }

                string json = await respuesta.Content.ReadAsStringAsync();

                try
                {
                    return ConvertirRespuesta(json);
                }
                catch (JsonException ex)
                {
                    throw new ClimaException(TipoErrorClima.FormatoInvalido, ex.Message, excepcionInterna: ex);
                }
            }
        }

        // convierto codigo http en uno de mis errores
        private static ClimaException CrearErrorHttp(HttpStatusCode codigo)
        {
            TipoErrorClima tipo;

            switch (codigo)
            {
                case HttpStatusCode.BadRequest:
                    tipo = TipoErrorClima.SolicitudInvalida;
                    break;

                case HttpStatusCode.NotFound:
                    tipo = TipoErrorClima.NoEncontrado;
                    break;

                case HttpStatusCode.InternalServerError:
                    tipo = TipoErrorClima.ErrorServidor;
                    break;

                default:
                    tipo = TipoErrorClima.ErrorHttp;
                    break;
            }
            
            return new ClimaException(tipo, $"El servidor respondió con el código HTTP {(int)codigo}", (int)codigo);
        }

        // Validamos el Json y lo convertimos a una lista de PronosticoDia; si algo no coincide lanzamos JsonException
        private static List<PronosticoDia> ConvertirRespuesta(string json)
        {
            var resultado = JsonSerializer.Deserialize<RespuestaClima>(json);

            var dias = resultado?.Dias;

            if (dias == null || dias.Fechas == null || dias.TemperaturasMinimas == null || dias.TemperaturasMaximas == null || dias.ProbabilidadesLluvia == null)
            {
                throw new JsonException("La respuesta no contiene los datos esperados");
            }

            int cantidad = dias.Fechas.Count;

            // Las listas deben tener un valor por cada fecha
            if (dias.TemperaturasMinimas.Count != cantidad || dias.TemperaturasMaximas.Count != cantidad || dias.ProbabilidadesLluvia.Count != cantidad)
            {
                throw new JsonException("Las listas del pronóstico tienen diferentes tamaños");
            }

            var pronostico = new List<PronosticoDia>();

            for (int i = 0; i<cantidad; i++)
            {
                bool fechaValida = DateTime.TryParseExact(
                    dias.Fechas[i],
                    "yyyy-MM-dd",
                    CultureInfo.InvariantCulture,
                    DateTimeStyles.None,
                    out DateTime fecha);

                if (!fechaValida || !dias.TemperaturasMinimas[i].HasValue || !dias.TemperaturasMaximas[i].HasValue || !dias.ProbabilidadesLluvia[i].HasValue)
                {
                    throw new JsonException("Hay un día con datos incompletos o una fecha inválida");
                }

                //Junto los valores de la misma posición
                pronostico.Add(new PronosticoDia
                {
                    Fecha = fecha,
                    TemperaturaMinima =
                        dias.TemperaturasMinimas[i]!.Value,
                    TemperaturaMaxima =
                        dias.TemperaturasMaximas[i]!.Value,
                    ProbabilidadLluvia =
                        dias.ProbabilidadesLluvia[i]!.Value
                });
            }

            return pronostico;
        }

    }
}