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
        private readonly IActividadRepository _actividadRepositorio;
        private readonly ISocioRepository _socioRepositorio;
        private readonly PrecioService _precioService;

        private string _nombreCategoria = string.Empty;
        private bool _incluyeVale;
        private string _nombreActividad = string.Empty;

        // Puede ser una categoría o una actividad: el panel de precios sirve para las dos
        private ConceptoConPrecio? _conceptoSeleccionado;
        private string _montoTexto = string.Empty;
        private Moneda _monedaSeleccionada = Moneda.ARS;
        private DateTime _fechaVigencia = DateTime.Today;

        private string _mensaje = "Cargando categorías y actividades...";
        private bool _estaOcupado;

        public ObservableCollection<ConceptoConPrecio> Categorias { get; } = new();

        public ObservableCollection<ConceptoConPrecio> Actividades { get; } = new();

        // Precios del concepto elegido, del más nuevo al más viejo
        public ObservableCollection<Precio> Historial { get; } = new();

        public List<Moneda> Monedas { get; } = new() { Moneda.ARS, Moneda.USD };

        // Las cuentas tienen su propio ViewModel; la pantalla lo muestra en una sección
        public CuentasViewModel Cuentas { get; }

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

        public string NombreActividad
        {
            get => _nombreActividad;
            set => SetProperty(ref _nombreActividad, value);
        }

        public ConceptoConPrecio? ConceptoSeleccionado
        {
            get => _conceptoSeleccionado;
            set
            {
                if (SetProperty(ref _conceptoSeleccionado, value))
                {
                    // Estas dos propiedades dependen del concepto elegido
                    OnPropertyChanged(nameof(HayConceptoSeleccionado));
                    OnPropertyChanged(nameof(TituloPrecio));
                }
            }
        }

        public bool HayConceptoSeleccionado => ConceptoSeleccionado != null;

        public string TituloPrecio
        {
            get
            {
                if (ConceptoSeleccionado == null)
                {
                    return string.Empty;
                }

                return ConceptoSeleccionado.Tipo == TipoConcepto.Cuota
                    ? $"Nuevo precio de la cuota: {ConceptoSeleccionado.Nombre}"
                    : $"Nuevo precio de la actividad: {ConceptoSeleccionado.Nombre}";
            }
        }

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
                    GuardarActividadCommand.ChangeCanExecute();
                    EliminarActividadCommand.ChangeCanExecute();
                    SeleccionarConceptoCommand.ChangeCanExecute();
                    GuardarPrecioCommand.ChangeCanExecute();
                }
            }
        }

        public Command CargarCommand { get; }
        public Command GuardarCategoriaCommand { get; }
        public Command<ConceptoConPrecio> EliminarCategoriaCommand { get; }
        public Command GuardarActividadCommand { get; }
        public Command<ConceptoConPrecio> EliminarActividadCommand { get; }
        public Command<ConceptoConPrecio> SeleccionarConceptoCommand { get; }
        public Command GuardarPrecioCommand { get; }
        public Command CancelarPrecioCommand { get; }

        public PreciosViewModel(
            ICategoriaRepository categoriaRepositorio,
            IActividadRepository actividadRepositorio,
            ISocioRepository socioRepositorio,
            PrecioService precioService,
            CuentasViewModel cuentas)
        {
            _categoriaRepositorio = categoriaRepositorio;
            _actividadRepositorio = actividadRepositorio;
            _socioRepositorio = socioRepositorio;
            _precioService = precioService;
            Cuentas = cuentas;

            CargarCommand = new Command(
                async () => await CargarAsync(),
                () => !EstaOcupado);

            GuardarCategoriaCommand = new Command(
                async () => await GuardarCategoriaAsync(),
                () => !EstaOcupado);

            EliminarCategoriaCommand = new Command<ConceptoConPrecio>(
                async item => await EliminarCategoriaAsync(item),
                item => item != null && !EstaOcupado);

            GuardarActividadCommand = new Command(
                async () => await GuardarActividadAsync(),
                () => !EstaOcupado);

            EliminarActividadCommand = new Command<ConceptoConPrecio>(
                async item => await EliminarActividadAsync(item),
                item => item != null && !EstaOcupado);

            SeleccionarConceptoCommand = new Command<ConceptoConPrecio>(
                async item => await SeleccionarConceptoAsync(item),
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

        // Dice si el item que me pasan es el que está abierto en el panel de precios
        private bool EsElSeleccionado(ConceptoConPrecio item)
        {
            return ConceptoSeleccionado != null
                && ConceptoSeleccionado.Tipo == item.Tipo
                && ConceptoSeleccionado.Id == item.Id;
        }

        private async Task ActualizarListasAsync()
        {
            var categorias = await _categoriaRepositorio.ObtenerTodosAsync();

            Categorias.Clear();

            foreach (var categoria in categorias)
            {
                // Pido el precio vigente a hoy: si hay uno con fecha futura todavía no cuenta
                var precio = await _precioService.ObtenerPrecioAsync(
                    TipoConcepto.Cuota, categoria.Id, DateTime.Today);

                Categorias.Add(new ConceptoConPrecio
                {
                    Tipo = TipoConcepto.Cuota,
                    Id = categoria.Id,
                    Nombre = categoria.Nombre,
                    DetalleTexto = categoria.IncluyeVale ? "Incluye vale de actividad" : "Sin vale",
                    PrecioTexto = ArmarTextoPrecio(precio),
                    Categoria = categoria
                });
            }

            var actividades = await _actividadRepositorio.ObtenerTodosAsync();

            Actividades.Clear();

            foreach (var actividad in actividades)
            {
                var precio = await _precioService.ObtenerPrecioAsync(
                    TipoConcepto.Actividad, actividad.Id, DateTime.Today);

                Actividades.Add(new ConceptoConPrecio
                {
                    Tipo = TipoConcepto.Actividad,
                    Id = actividad.Id,
                    Nombre = actividad.Nombre,
                    DetalleTexto = actividad.Activa ? "Activa" : "Inactiva",
                    PrecioTexto = ArmarTextoPrecio(precio),
                    Actividad = actividad
                });
            }
        }

        private async Task ActualizarHistorialAsync()
        {
            Historial.Clear();

            if (ConceptoSeleccionado == null)
            {
                return;
            }

            var precios = await _precioService.ObtenerHistorialAsync(
                ConceptoSeleccionado.Tipo, ConceptoSeleccionado.Id);

            foreach (var precio in precios)
            {
                Historial.Add(precio);
            }
        }

        private async Task CargarAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                await ActualizarListasAsync();
                await Cuentas.CargarAsync();
                Mensaje = $"Categorías: {Categorias.Count}. Actividades: {Actividades.Count}.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudieron cargar los datos.";
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

                await ActualizarListasAsync();
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

        private async Task EliminarCategoriaAsync(ConceptoConPrecio item)
        {
            if (item == null || item.Categoria == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                // No dejo borrar una categoría que todavía tiene socios asignados
                var socios = await _socioRepositorio.ObtenerTodosAsync();

                if (socios.Any(socio => socio.CategoriaId == item.Id))
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
                if (EsElSeleccionado(item))
                {
                    CancelarPrecio();
                }

                await ActualizarListasAsync();
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

        private async Task GuardarActividadAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            // Uso el mismo validador, pasándole que se trata de una actividad
            string? error = ValidadorCategoria.Validar(
                NombreActividad,
                Actividades.Select(actividad => actividad.Nombre),
                "actividad");

            if (error != null)
            {
                Mensaje = error;
                return;
            }

            EstaOcupado = true;

            try
            {
                var actividad = new Actividad
                {
                    Nombre = NombreActividad.Trim()
                };

                await _actividadRepositorio.GuardarAsync(actividad);

                NombreActividad = string.Empty;

                await ActualizarListasAsync();
                Mensaje = "Actividad agregada. Ahora podés cargarle un precio.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo guardar la actividad.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task EliminarActividadAsync(ConceptoConPrecio item)
        {
            if (item == null || item.Actividad == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                bool confirmar = await Shell.Current.DisplayAlert(
                    "Eliminar actividad",
                    $"¿Querés eliminar la actividad {item.Nombre}? Los pagos ya registrados se conservan.",
                    "Eliminar",
                    "Cancelar");

                if (!confirmar)
                {
                    return;
                }

                await _actividadRepositorio.EliminarAsync(item.Actividad);

                if (EsElSeleccionado(item))
                {
                    CancelarPrecio();
                }

                await ActualizarListasAsync();
                Mensaje = "Actividad eliminada.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo eliminar la actividad.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task SeleccionarConceptoAsync(ConceptoConPrecio item)
        {
            if (item == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                ConceptoSeleccionado = item;
                MontoTexto = string.Empty;
                FechaVigencia = DateTime.Today;

                await ActualizarHistorialAsync();
                Mensaje = $"Cargá el nuevo precio de {item.Nombre} (el formulario está arriba).";
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

            if (ConceptoSeleccionado == null)
            {
                Mensaje = "Elegí una categoría o una actividad.";
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
                    ConceptoSeleccionado.Tipo,
                    ConceptoSeleccionado.Id,
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
                await ActualizarListasAsync();
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
            ConceptoSeleccionado = null;
            MontoTexto = string.Empty;
            Historial.Clear();
        }
    }
}