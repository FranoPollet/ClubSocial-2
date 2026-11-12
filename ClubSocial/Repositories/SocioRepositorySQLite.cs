using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public class SocioRepositorySQLite : ISocioRepository
    {
        private readonly SQLiteAsyncConnection _conexion;
        private readonly Task _inicializacion;

        // Constructor que recibe la ruta de la base de datos SQLite.
        public SocioRepositorySQLite(string rutaBaseDatos) : this(new SQLiteAsyncConnection(rutaBaseDatos))
        {
        }

        // Constructor q recibe la conexion ya armada
        public SocioRepositorySQLite(SQLiteAsyncConnection conexion)
        {
            _conexion = conexion;

            //Si ya existe sqlite conserva los datos
            _inicializacion = _conexion.CreateTableAsync<Socio>();
        }

        // Implementación de los métodos de la interfaz ISocioRepository.
        public async Task<List<Socio>> ObtenerTodosAsync()
        {
            // Esperamos a que la tabla esté lista antes de consultarla.
            await _inicializacion;

            return await _conexion.Table<Socio>()
                .OrderBy(socio => socio.Apellido)
                .ToListAsync();
        }

        // Implementación del método para obtener un socio por su DNI.
        public async Task<Socio?> ObtenerPorDniAsync(string dni)
        {
            await _inicializacion;

            return await _conexion.Table<Socio>()
                .Where(socio => socio.Dni == dni)
                .FirstOrDefaultAsync();
        }

        // Implementación del método para guardar un socio (insertar o actualizar).
        public async Task<int> GuardarAsync(Socio socio)
        {
            await _inicializacion;

            // Un socio nuevo todavía no tiene un Id asignado.
            if (socio.Id == 0)
            {
                return await _conexion.InsertAsync(socio);
            }

            return await _conexion.UpdateAsync(socio);
        }

        // Implementación del método para eliminar un socio.
        public async Task<int> EliminarAsync(Socio socio)
        {
            await _inicializacion;

            return await _conexion.DeleteAsync(socio);
        }
    }
}
