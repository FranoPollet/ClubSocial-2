using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public interface IGastoRepository
    {
        // Todos los gastos de un mes (el período es el primer día del mes)
        Task<List<Gasto>> ObtenerPorPeriodoAsync(DateTime periodo);

        Task<int> GuardarAsync(Gasto gasto);

        Task<int> EliminarAsync(Gasto gasto);
    }
}