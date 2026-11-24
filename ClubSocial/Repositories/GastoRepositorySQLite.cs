using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public class GastoRepositorySQLite : IGastoRepository
    {
        private readonly SQLiteAsyncConnection _conexion;
        private readonly Task _inicializacion;

        // Constructor que usa la app: recibe la ruta de la base de datos
        public GastoRepositorySQLite(string rutaBaseDatos) : this(new SQLiteAsyncConnection(rutaBaseDatos))
        { }

        // Constructor que usan los tests: recibe la conexión ya armada
        public GastoRepositorySQLite(SQLiteAsyncConnection conexion)
        {
            _conexion = conexion;
            _inicializacion = _conexion.CreateTableAsync<Gasto>();
        }

        public async Task<List<Gasto>> ObtenerPorPeriodoAsync(DateTime periodo)
        {
            await _inicializacion;

            return await _conexion.Table<Gasto>()
                .Where(gasto => gasto.Periodo == periodo)
                .OrderBy(gasto => gasto.Fecha)
                .ToListAsync();
        }

        public async Task<int> GuardarAsync(Gasto gasto)
        {
            await _inicializacion;

            // Un gasto nuevo todavía no tiene Id
            if (gasto.Id == 0)
            {
                return await _conexion.InsertAsync(gasto);
            }

            return await _conexion.UpdateAsync(gasto);
        }

        public async Task<int> EliminarAsync(Gasto gasto)
        {
            await _inicializacion;

            return await _conexion.DeleteAsync(gasto);
        }
    }
}