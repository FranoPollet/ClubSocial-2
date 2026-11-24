using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public class CuentaRepositorySQLite : ICuentaRepository
    {
        private readonly SQLiteAsyncConnection _conexion;
        private readonly Task _inicializacion;

        // Constructor que usa la app: recibe la ruta de la base de datos
        public CuentaRepositorySQLite(string rutaBaseDatos) : this(new SQLiteAsyncConnection(rutaBaseDatos))
        { }

        // Constructor que usan los tests: recibe la conexión ya armada
        public CuentaRepositorySQLite(SQLiteAsyncConnection conexion)
        {
            _conexion = conexion;
            _inicializacion = _conexion.CreateTableAsync<Cuenta>();
        }

        public async Task<List<Cuenta>> ObtenerTodosAsync()
        {
            await _inicializacion;

            return await _conexion.Table<Cuenta>()
                .OrderBy(cuenta => cuenta.Nombre)
                .ToListAsync();
        }

        public async Task<int> GuardarAsync(Cuenta cuenta)
        {
            await _inicializacion;

            // Una cuenta nueva todavía no tiene Id
            if (cuenta.Id == 0)
            {
                return await _conexion.InsertAsync(cuenta);
            }

            return await _conexion.UpdateAsync(cuenta);
        }

        public async Task<int> EliminarAsync(Cuenta cuenta)
        {
            await _inicializacion;

            return await _conexion.DeleteAsync(cuenta);
        }
    }
}