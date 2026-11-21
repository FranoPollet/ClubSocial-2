using SQLite;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public class PagoRepositorySQLite : IPagoRepository
    {
        private readonly SQLiteAsyncConnection _conexion;
        private readonly Task _inicializacion;

        // Constructor que usa la app: recibe la ruta de la base de datos
        public PagoRepositorySQLite(string rutaBaseDatos) : this(new SQLiteAsyncConnection(rutaBaseDatos))
        { }

        // Constructor que usan los tests: recibe la conexión ya armada
        public PagoRepositorySQLite(SQLiteAsyncConnection conexion)
        {
            _conexion = conexion;
            _inicializacion = _conexion.CreateTableAsync<Pago>();
        }

        public async Task<List<Pago>> ObtenerPorPeriodoAsync(DateTime periodo)
        {
            await _inicializacion;

            return await _conexion.Table<Pago>()
                .Where(pago => pago.Periodo == periodo)
                .ToListAsync();
        }

        public async Task<int> GuardarAsync(Pago pago)
        {
            await _inicializacion;

            // Un pago nuevo todavía no tiene Id
            if (pago.Id == 0)
            {
                return await _conexion.InsertAsync(pago);
            }

            return await _conexion.UpdateAsync(pago);
        }

        public async Task<int> EliminarAsync(Pago pago)
        {
            await _inicializacion;

            return await _conexion.DeleteAsync(pago);
        }
    }
}
