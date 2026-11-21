using SQLite;
using System.Collections.Generic;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public class ActividadRepositorySQLite : IActividadRepository
    {
        private readonly SQLiteAsyncConnection _conexion;
        private readonly Task _inicializacion;

        // Constructor que usa la app: recibe la ruta de la base de datos
        public ActividadRepositorySQLite(string rutaBaseDatos) : this(new SQLiteAsyncConnection(rutaBaseDatos))
        { }

        // Constructor que usan los tests: recibe la conexión ya armada
        public ActividadRepositorySQLite(SQLiteAsyncConnection conexion)
        {
            _conexion = conexion;
            _inicializacion = _conexion.CreateTableAsync<Actividad>();
        }

        public async Task<List<Actividad>> ObtenerTodosAsync()
        {
            // espero a que la tabla esté lista antes de consultarla
            await _inicializacion;

            return await _conexion.Table<Actividad>()
                .OrderBy(actividad => actividad.Nombre)
                .ToListAsync();
        }

        public async Task<int> GuardarAsync(Actividad actividad)
        {
            await _inicializacion;

            // Una actividad nueva todavía no tiene Id
            if (actividad.Id == 0)
            {
                return await _conexion.InsertAsync(actividad);
            }

            return await _conexion.UpdateAsync(actividad);
        }

        public async Task<int> EliminarAsync(Actividad actividad)
        {
            await _inicializacion;

            return await _conexion.DeleteAsync(actividad);
        }
    }

}
