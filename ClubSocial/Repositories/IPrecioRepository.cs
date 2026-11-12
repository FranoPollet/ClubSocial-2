using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public interface IPrecioRepository
    {
        //Devuelve el precio que estaba en esa fecha (null si todavía no había ninguno)
        Task<Precio?> ObtenerVigenteAsync(TipoConcepto tipo, int conceptoid, DateTime fecha);

        //Todos los precios de un concepto, del más nuevo al más viejo
        Task<List<Precio>> ObtenerHistorialAsync(TipoConcepto tipo, int conceptoId);

        Task<int> GuardarAsync(Precio precio);
    }
}