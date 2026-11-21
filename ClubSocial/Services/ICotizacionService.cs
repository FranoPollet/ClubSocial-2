using System.Threading.Tasks;

namespace ClubSocial.Services
{
    public interface ICotizacionService
    {
        // Valor de venta del dólar blue. Lanza CotizacionException si no se pudo obtener.
        Task<decimal> ObtenerDolarBlueAsync();
    }
}