using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;

namespace ClubSocial.Repositories
{
    public class CategoriaRepositorySQLite : ICategoriaRepository
    {
        private readonly SQLiteAsyncConnection _conexion;
        private readonly Task _inicializacion;

        //Constructor que usa la app donde recibirá la ruta de la base de datos
        public CategoriaRepositorySQLite(string rutaBaseDatos) : this(new SQLiteAsyncConnection(rutaBaseDatos))
        { }

        //Constructor q recibe la conexión ya armada
        public CategoriaRepositorySQLite(SQLiteAsyncConnection conexion)
        {
            _conexion = conexion;

            //Si la tabla ya existe entonces sqlite conserva los datos
            _inicializacion = _conexion.CreateTableAsync<Categoria>();
        }

        public async Task<List<Categoria>> ObtenerTodosAsync()
        {
            //espero q la tabla este lista antes de consultarla
            await _inicializacion;

            return await _conexion.Table<Categoria>().OrderBy(categoria => categoria.Nombre).ToListAsync();
        }

        public async Task<int> GuardarAsync(Categoria categoria)
        {
            await _inicializacion;

            //Una categoría nueva todavía no tiene un id asignado
            if (categoria.Id == 0)
            {
                return await _conexion.InsertAsync(categoria);
            }

            return await _conexion.UpdateAsync(categoria);
        }

        public async Task<int> EliminarAsync(Categoria categoria)
        {
            await _inicializacion;

            return await _conexion.DeleteAsync(categoria);
        }
    }
}