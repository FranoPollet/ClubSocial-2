using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public interface IPagoRepository
    {
        // Todos los pagos de un mes (el período es el primer día del mes)
        Task<List<Pago>> ObtenerPorPeriodoAsync(DateTime periodo);

        Task<int> GuardarAsync(Pago pago);

        Task<int> EliminarAsync(Pago pago);
    }
}