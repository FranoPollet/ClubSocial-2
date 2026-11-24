using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public interface ICuentaRepository
    {
        Task<List<Cuenta>> ObtenerTodosAsync();

        Task<int> GuardarAsync(Cuenta cuenta);

        Task<int> EliminarAsync(Cuenta cuenta);
    }
}