using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public interface IActividadRepository
    {
        Task<List<Actividad>> ObtenerTodosAsync();

        Task<int> GuardarAsync(Actividad actividad);

        Task<int> EliminarAsync(Actividad actividad);
    }
}