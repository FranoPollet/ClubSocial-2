using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;
using ClubSocial.Services;
using Microsoft.Maui.Controls;

namespace ClubSocial.ViewModels
{
    public class PreciosViewModel : BaseViewModel
    {
        private readonly ICategoriaRepository _categoriaRepositorio;
        private readonly ISocioRepository _socioRepositorio;
        private readonly PrecioService _precioService;

        private string _nombreCategoria = string.Empty;
        private bool _incluyeVale;

        private CategoriaConPrecio? _categoriaSeleccionada;
        private string _montoTexto = string.Empty;
        private Moneda _monedaSeleccionada = Moneda.ARS;
        private DateTime _fechaVigencia = DateTime.Today;

        private string _mensaje = "Cargando categorías...";
        private bool _estaOcupado;

        public ObservableCollection<CategoriaConPrecio> Categorias { get; } = new();

        // Precios de la categoría elegida, del más nuevo al más viejo
        public ObservableCollection<Precio> Historial { get; } = new();

        public List<Moneda> Monedas { get; } = new() { Moneda.ARS, Moneda.USD };

        public string NombreCategoria
        {
            get => _nombreCategoria;
            set => SetProperty(ref _nombreCategoria, value);
        }

        public bool IncluyeVale
        {
            get => _incluyeVale;
            set => SetProperty(ref _incluyeVale, value);
        }

        public CategoriaConPrecio? CategoriaSeleccionada
        {
            get => _categoriaSeleccionada;
            set
            {
                if (SetProperty(ref _categoriaSeleccionada, value))
                {
                    // Estas dos propiedades dependen de la categoría elegida
                    OnPropertyChanged(nameof(HayCategoriaSeleccionada));
                    OnPropertyChanged(nameof(TituloPrecio));
                }
            }
        }

        public bool HayCategoriaSeleccionada => CategoriaSeleccionada != null;

        public string TituloPrecio => CategoriaSeleccionada == null
            ? string.Empty
            : $"Nuevo precio de la cuota: {CategoriaSeleccionada.Nombre}";

        public string MontoTexto
        {
            get => _montoTexto;
            set => SetProperty(ref _montoTexto, value);
        }

        public Moneda MonedaSeleccionada
        {
            get => _monedaSeleccionada;
            set => SetProperty(ref _monedaSeleccionada, value);
        }

        public DateTime FechaVigencia
        {
            get => _fechaVigencia;
            set => SetProperty(ref _fechaVigencia, value);
        }

        public string Mensaje
        {
            get => _mensaje;
            set => SetProperty(ref _mensaje, value);
        }

        public bool EstaOcupado
        {
            get => _estaOcupado;
            set
            {
                if (SetProperty(ref _estaOcupado, value))
                {
                    // Mientras guardamos o cargamos, evitamos otra operación.
                    CargarCommand.ChangeCanExecute();
                    GuardarCategoriaCommand.ChangeCanExecute();
                    EliminarCategoriaCommand.ChangeCanExecute();
                    SeleccionarCategoriaCommand.ChangeCanExecute();
                    GuardarPrecioCommand.ChangeCanExecute();
                }
            }
        }

        public Command CargarCommand { get; }
        public Command GuardarCategoriaCommand { get; }
        public Command<CategoriaConPrecio> EliminarCategoriaCommand { get; }
        public Command<CategoriaConPrecio> SeleccionarCategoriaCommand { get; }
        public Command GuardarPrecioCommand { get; }
        public Command CancelarPrecioCommand { get; }

        public PreciosViewModel(
            ICategoriaRepository categoriaRepositorio,
            ISocioRepository socioRepositorio,
            PrecioService precioService)
        {
            _categoriaRepositorio = categoriaRepositorio;
            _socioRepositorio = socioRepositorio;
            _precioService = precioService;

            CargarCommand = new Command(
                async () => await CargarCategoriasAsync(),
                () => !EstaOcupado);

            GuardarCategoriaCommand = new Command(
                async () => await GuardarCategoriaAsync(),
                () => !EstaOcupado);

            EliminarCategoriaCommand = new Command<CategoriaConPrecio>(
                async item => await EliminarCategoriaAsync(item),
                item => item != null && !EstaOcupado);

            SeleccionarCategoriaCommand = new Command<CategoriaConPrecio>(
                async item => await SeleccionarCategoriaAsync(item),
                item => item != null && !EstaOcupado);

            GuardarPrecioCommand = new Command(
                async () => await GuardarPrecioAsync(),
                () => !EstaOcupado);

            CancelarPrecioCommand = new Command(CancelarPrecio);
        }

        // Arma el texto del precio que rige hoy
        private static string ArmarTextoPrecio(Precio? precio)
        {
            if (precio == null)
            {
                return "Sin precio cargado";
            }

            return $"{precio.Moneda} {precio.Monto:N2} (desde {precio.VigenteDesde:dd/MM/yyyy})";
        }

        private async Task ActualizarCategoriasAsync()
        {
            var categorias = await _categoriaRepositorio.ObtenerTodosAsync();

            Categorias.Clear();

            foreach (var categoria in categorias)
            {
                // Pido el precio vigente a hoy: si hay uno con fecha futura todavía no cuenta
                var precio = await _precioService.ObtenerPrecioAsync(
                    TipoConcepto.Cuota, categoria.Id, DateTime.Today);

                Categorias.Add(new CategoriaConPrecio
                {
                    Categoria = categoria,
                    PrecioTexto = ArmarTextoPrecio(precio)
                });
            }
        }

        private async Task ActualizarHistorialAsync()
        {
            Historial.Clear();

            if (CategoriaSeleccionada == null)
            {
                return;
            }

            var precios = await _precioService.ObtenerHistorialAsync(
                TipoConcepto.Cuota, CategoriaSeleccionada.Categoria.Id);

            foreach (var precio in precios)
            {
                Historial.Add(precio);
            }
        }

        private async Task CargarCategoriasAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                await ActualizarCategoriasAsync();
                Mensaje = $"Categorías registradas: {Categorias.Count}.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudieron cargar las categorías.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task GuardarCategoriaAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            // La regla de validación está en ValidadorCategoria
            string? error = ValidadorCategoria.Validar(
                NombreCategoria,
                Categorias.Select(categoria => categoria.Nombre));

            if (error != null)
            {
                Mensaje = error;
                return;
            }

            EstaOcupado = true;

            try
            {
                var categoria = new Categoria
                {
                    Nombre = NombreCategoria.Trim(),
                    IncluyeVale = IncluyeVale
                };

                await _categoriaRepositorio.GuardarAsync(categoria);

                NombreCategoria = string.Empty;
                IncluyeVale = false;

                await ActualizarCategoriasAsync();
                Mensaje = "Categoría agregada. Ahora podés cargarle un precio.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo guardar la categoría.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task EliminarCategoriaAsync(CategoriaConPrecio item)
        {
            if (item == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                // No dejo borrar una categoría que todavía tiene socios asignados
                var socios = await _socioRepositorio.ObtenerTodosAsync();

                if (socios.Any(socio => socio.CategoriaId == item.Categoria.Id))
                {
                    Mensaje = "No se puede eliminar: hay socios en esa categoría.";
                    return;
                }

                bool confirmar = await Shell.Current.DisplayAlert(
                    "Eliminar categoría",
                    $"¿Querés eliminar la categoría {item.Nombre}?",
                    "Eliminar",
                    "Cancelar");

                if (!confirmar)
                {
                    return;
                }

                await _categoriaRepositorio.EliminarAsync(item.Categoria);

                // Si estaba abierto el panel de precios de esa categoría, lo cierro
                if (CategoriaSeleccionada?.Categoria.Id == item.Categoria.Id)
                {
                    CancelarPrecio();
                }

                await ActualizarCategoriasAsync();
                Mensaje = "Categoría eliminada.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo eliminar la categoría.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task SeleccionarCategoriaAsync(CategoriaConPrecio item)
        {
            if (item == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                CategoriaSeleccionada = item;
                MontoTexto = string.Empty;
                FechaVigencia = DateTime.Today;

                await ActualizarHistorialAsync();
                Mensaje = $"Cargá el nuevo precio de {item.Nombre}.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo cargar el historial de precios.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task GuardarPrecioAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            if (CategoriaSeleccionada == null)
            {
                Mensaje = "Elegí una categoría.";
                return;
            }

            if (!PrecioService.TryParsearMonto(MontoTexto, out decimal monto))
            {
                Mensaje = "Ingresá un monto válido, sin puntos. Los decimales van con coma.";
                return;
            }

            EstaOcupado = true;

            try
            {
                // El servicio valida el monto y que la fecha sea posterior al último precio
                string? error = await _precioService.CambiarPrecioAsync(
                    TipoConcepto.Cuota,
                    CategoriaSeleccionada.Categoria.Id,
                    monto,
                    MonedaSeleccionada,
                    FechaVigencia);

                if (error != null)
                {
                    Mensaje = error;
                    return;
                }

                MontoTexto = string.Empty;

                await ActualizarHistorialAsync();
                await ActualizarCategoriasAsync();
                Mensaje = "Precio guardado.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo guardar el precio.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private void CancelarPrecio()
        {
            CategoriaSeleccionada = null;
            MontoTexto = string.Empty;
            Historial.Clear();
        }
    }
}