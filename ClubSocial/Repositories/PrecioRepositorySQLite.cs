using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public class PrecioRepositorySQLite : IPrecioRepository
    {
        private readonly SQLiteAsyncConnection _conexion;
        private readonly Task _inicializacion;

        //Constructor q recibe ruta de base de datos
        public PrecioRepositorySQLite(string rutaBaseDatos) : this(new SQLiteAsyncConnection(rutaBaseDatos))
        {}

        // Constructor q recibe la conexión ya armada
        public PrecioRepositorySQLite(SQLiteAsyncConnection conexion)
        {
            _conexion = conexion;
            _inicializacion = _conexion.CreateTableAsync<Precio>();
        }

        public async Task<Precio?> ObtenerVigenteAsync(TipoConcepto tipo, int conceptoId, DateTime fecha)
        {
            await _inicializacion;

            // entre todos los precios q ya estaban en esa fecha, me quedo con el q emepzo mas reciente
            return await _conexion.Table<Precio>()
                .Where(precio => precio.Tipo == tipo 
                                 && precio.ConceptoId == conceptoId
                                 && precio.VigenteDesde <= fecha)
                .OrderByDescending(precio => precio.VigenteDesde)
                .FirstOrDefaultAsync();
        }

        public async Task<List<Precio>> ObtenerHistorialAsync(TipoConcepto tipo, int conceptoId)
        {
            await _inicializacion;

            return await _conexion.Table<Precio>()
                .Where(precio => precio.Tipo == tipo
                                && precio.ConceptoId == conceptoId)
                .OrderByDescending(precio => precio.VigenteDesde)
                .ToListAsync();
        }

        public async Task<int> GuardarAsync(Precio precio)
        {
            await _inicializacion;

            //Guardo solo la fecha para q el cambio de precio empiece con el día

            precio.VigenteDesde = precio.VigenteDesde.Date;

            if (precio.Id == 0)
            {
                return await _conexion.InsertAsync(precio);
            }

            return await _conexion.UpdateAsync(precio);
        }
    }
}