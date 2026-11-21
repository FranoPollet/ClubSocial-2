using System.Text.Json.Serialization;

namespace ClubSocial.Models
{
    // Respuesta de DolarApi. Del JSON solo me interesa el valor de venta. 
    public class RespuestaDolar
    {
        [JsonPropertyName("venta")]
        public decimal? Venta { get; set; }
    }
}