using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public interface ICategoriaRepository
    {
        Task<List<Categoria>> ObtenerTodosAsync();

        Task<int> GuardarAsync(Categoria categoria);

        Task<int> EliminarAsync(Categoria categoria);
    }
}
