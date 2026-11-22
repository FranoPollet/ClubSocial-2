using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;
using ClubSocial.Services;
using Microsoft.Maui.Controls;

namespace ClubSocial.ViewModels
{
    public class CobranzaViewModel : BaseViewModel
    {
        private readonly ISocioRepository _socioRepositorio;
        private readonly ICategoriaRepository _categoriaRepositorio;
        private readonly IActividadRepository _actividadRepositorio;
        private readonly IPagoRepository _pagoRepositorio;
        private readonly CobranzaService _cobranzaService;
        private readonly PrecioService _precioService;
        private readonly ICotizacionService _cotizacionService;

        // Opción "Todas" del filtro de categorías (con Id 0 para distinguirla)
        private readonly Categoria _todasLasCategorias = new Categoria { Id = 0, Nombre = "Todas" };

        // Todos los socios activos del mes; la lista que se ve es esta misma, filtrada
        private List<SocioCobranza> _todosLosSocios = new();

        private DateTime _periodo = CobranzaService.PrimerDiaDelMes(DateTime.Today);
        private string _resumenTexto = string.Empty;

        // Filtros
        private string _busqueda = string.Empty;
        private Categoria? _filtroCategoria;
        private string _filtroEstado = "Todos";

        // Datos del cobro que se está armando
        private SocioCobranza? _socioElegido;
        private bool _esActividad;
        private DateTime _fechaPago = DateTime.Today;
        private Actividad? _actividadSeleccionada;
        private bool _valeDisponible;
        private bool _usarVale;
        private Precio? _precioPrevisto;
        private string _precioPrevistoTexto = string.Empty;
        private bool _pideCotizacion;
        private string _cotizacionTexto = string.Empty;

        // Mientras armo el cobro por código no quiero que cada cambio recalcule el precio
        private bool _ajustandoCobro;

        private string _mensaje = "Cargando cobranza...";
        private bool _estaOcupado;

        public ObservableCollection<SocioCobranza> SociosCobranza { get; } = new();

        public ObservableCollection<PagoItem> PagosDelMes { get; } = new();

        public ObservableCollection<Categoria> CategoriasFiltro { get; } = new();

        public ObservableCollection<Actividad> ActividadesDisponibles { get; } = new();

        public List<string> Estados { get; } = new() { "Todos", "Pagaron", "Deben" };

        public DateTime Periodo
        {
            get => _periodo;
            set
            {
                if (SetProperty(ref _periodo, value))
                {
                    OnPropertyChanged(nameof(PeriodoTexto));
                }
            }
        }

        public string PeriodoTexto
        {
            get
            {
                string mes = _periodo.ToString("MMMM yyyy", new CultureInfo("es-AR"));

                // Le pongo la primera letra en mayúscula
                return char.ToUpper(mes[0]) + mes.Substring(1);
            }
        }

        public string ResumenTexto
        {
            get => _resumenTexto;
            set => SetProperty(ref _resumenTexto, value);
        }

        public string Busqueda
        {
            get => _busqueda;
            set
            {
                if (SetProperty(ref _busqueda, value))
                {
                    AplicarFiltros();
                }
            }
        }

        public Categoria? FiltroCategoria
        {
            get => _filtroCategoria;
            set
            {
                if (SetProperty(ref _filtroCategoria, value))
                {
                    AplicarFiltros();
                }
            }
        }

        public string FiltroEstado
        {
            get => _filtroEstado;
            set
            {
                // Al armarse, el Picker puede mandar null: en ese caso me quedo con el valor anterior
                if (value == null)
                {
                    return;
                }

                if (SetProperty(ref _filtroEstado, value))
                {
                    AplicarFiltros();
                }
            }
        }

        public SocioCobranza? SocioElegido
        {
            get => _socioElegido;
            set
            {
                if (SetProperty(ref _socioElegido, value))
                {
                    OnPropertyChanged(nameof(HayCobroAbierto));
                    OnPropertyChanged(nameof(TituloCobro));
                }
            }
        }

        public bool HayCobroAbierto => SocioElegido != null;

        public string TituloCobro
        {
            get
            {
                if (SocioElegido == null)
                {
                    return string.Empty;
                }

                return EsActividad
                    ? $"Cobrar actividad a {SocioElegido.NombreCompleto}"
                    : $"Cobrar cuota a {SocioElegido.NombreCompleto}";
            }
        }

        public bool EsActividad
        {
            get => _esActividad;
            set
            {
                if (SetProperty(ref _esActividad, value))
                {
                    OnPropertyChanged(nameof(TituloCobro));
                    OnPropertyChanged(nameof(MostrarVale));
                }
            }
        }

        public DateTime FechaPago
        {
            get => _fechaPago;
            set
            {
                if (SetProperty(ref _fechaPago, value) && !_ajustandoCobro)
                {
                    // El precio puede ser distinto según la fecha, así que lo calculo de nuevo
                    ActualizarPrevistaCommand.Execute(null);
                }
            }
        }

        public Actividad? ActividadSeleccionada
        {
            get => _actividadSeleccionada;
            set
            {
                if (SetProperty(ref _actividadSeleccionada, value) && !_ajustandoCobro)
                {
                    ActualizarPrevistaCommand.Execute(null);
                }
            }
        }

        public bool ValeDisponible
        {
            get => _valeDisponible;
            set
            {
                if (SetProperty(ref _valeDisponible, value))
                {
                    OnPropertyChanged(nameof(MostrarVale));
                }
            }
        }

        // El interruptor del vale solo se ve si es una actividad y el socio tiene vale libre
        public bool MostrarVale => EsActividad && ValeDisponible;

        public bool UsarVale
        {
            get => _usarVale;
            set
            {
                if (SetProperty(ref _usarVale, value) && !_ajustandoCobro)
                {
                    ActualizarPrevistaCommand.Execute(null);
                }
            }
        }

        public string PrecioPrevistoTexto
        {
            get => _precioPrevistoTexto;
            set => SetProperty(ref _precioPrevistoTexto, value);
        }

        public bool PideCotizacion
        {
            get => _pideCotizacion;
            set => SetProperty(ref _pideCotizacion, value);
        }

        public string CotizacionTexto
        {
            get => _cotizacionTexto;
            set
            {
                if (SetProperty(ref _cotizacionTexto, value))
                {
                    OnPropertyChanged(nameof(TotalPesosTexto));
                }
            }
        }

        // Lo que se va a cobrar en pesos
        public string TotalPesosTexto
        {
            get
            {
                if (_precioPrevisto == null)
                {
                    return string.Empty;
                }

                if (!PideCotizacion)
                {
                    return $"Total a cobrar: ARS {_precioPrevisto.Monto:N2}";
                }

                if (!PrecioService.TryParsearMonto(CotizacionTexto, out decimal cotizacion))
                {
                    return "Total a cobrar: escribí la cotización";
                }

                return $"Total a cobrar: ARS {Math.Round(_precioPrevisto.Monto * cotizacion, 2):N2}";
            }
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
                    // Mientras cargamos o guardamos, evitamos otra operación.
                    CargarCommand.ChangeCanExecute();
                    MesAnteriorCommand.ChangeCanExecute();
                    MesSiguienteCommand.ChangeCanExecute();
                    CobrarCuotaCommand.ChangeCanExecute();
                    CobrarActividadCommand.ChangeCanExecute();
                    RegistrarCobroCommand.ChangeCanExecute();
                    AnularPagoCommand.ChangeCanExecute();
                }
            }
        }

        public Command CargarCommand { get; }
        public Command MesAnteriorCommand { get; }
        public Command MesSiguienteCommand { get; }
        public Command<SocioCobranza> CobrarCuotaCommand { get; }
        public Command<SocioCobranza> CobrarActividadCommand { get; }
        public Command RegistrarCobroCommand { get; }
        public Command CancelarCobroCommand { get; }
        public Command TraerDolarCommand { get; }
        public Command ActualizarPrevistaCommand { get; }
        public Command<PagoItem> AnularPagoCommand { get; }

        public CobranzaViewModel(
            ISocioRepository socioRepositorio,
            ICategoriaRepository categoriaRepositorio,
            IActividadRepository actividadRepositorio,
            IPagoRepository pagoRepositorio,
            CobranzaService cobranzaService,
            PrecioService precioService,
            ICotizacionService cotizacionService)
        {
            _socioRepositorio = socioRepositorio;
            _categoriaRepositorio = categoriaRepositorio;
            _actividadRepositorio = actividadRepositorio;
            _pagoRepositorio = pagoRepositorio;
            _cobranzaService = cobranzaService;
            _precioService = precioService;
            _cotizacionService = cotizacionService;

            // Empieza con "Todas" elegida
            _filtroCategoria = _todasLasCategorias;

            CargarCommand = new Command(
                async () => await CargarAsync(),
                () => !EstaOcupado);

            MesAnteriorCommand = new Command(
                async () => await CambiarPeriodoAsync(-1),
                () => !EstaOcupado);

            MesSiguienteCommand = new Command(
                async () => await CambiarPeriodoAsync(1),
                () => !EstaOcupado);

            CobrarCuotaCommand = new Command<SocioCobranza>(
                async item => await AbrirCobroAsync(item, false),
                item => item != null && !EstaOcupado);

            CobrarActividadCommand = new Command<SocioCobranza>(
                async item => await AbrirCobroAsync(item, true),
                item => item != null && !EstaOcupado);

            RegistrarCobroCommand = new Command(
                async () => await RegistrarCobroAsync(),
                () => !EstaOcupado);

            CancelarCobroCommand = new Command(CerrarCobro);

            TraerDolarCommand = new Command(
                async () => await TraerDolarAsync());

            ActualizarPrevistaCommand = new Command(
                async () => await ActualizarPrevistaAsync());

            AnularPagoCommand = new Command<PagoItem>(
                async item => await AnularPagoAsync(item),
                item => item != null && !EstaOcupado);
        }

        // ---------- Carga de datos ----------

        private async Task CargarAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                await ActualizarDatosAsync();

                Mensaje = _todosLosSocios.Count == 0
                      ? "No hay socios activos. Cargalos en la pestaña Socios."
                      : $"Socios activos: {_todosLosSocios.Count}. Tocá Cobrar cuota o Cobrar actividad.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo cargar la cobranza.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task ActualizarDatosAsync()
        {
            var categorias = await _categoriaRepositorio.ObtenerTodosAsync();
            var actividades = await _actividadRepositorio.ObtenerTodosAsync();
            var socios = await _socioRepositorio.ObtenerTodosAsync();
            var pagos = await _pagoRepositorio.ObtenerPorPeriodoAsync(Periodo);

            ActualizarFiltroCategorias(categorias);

            // Si hay un cobro abierto no toco las actividades, para no perder la que eligió
            if (!HayCobroAbierto)
            {
                ActividadesDisponibles.Clear();

                foreach (var actividad in actividades.Where(a => a.Activa))
                {
                    ActividadesDisponibles.Add(actividad);
                }
            }

            _todosLosSocios = socios
                .Where(socio => socio.Activo)
                .Select(socio => new SocioCobranza
                {
                    Socio = socio,
                    Categoria = categorias.FirstOrDefault(c => c.Id == socio.CategoriaId),
                    PagoCuota = pagos.FirstOrDefault(p => p.SocioId == socio.Id && p.Tipo == TipoConcepto.Cuota),
                    CantidadActividades = pagos.Count(p => p.SocioId == socio.Id && p.Tipo == TipoConcepto.Actividad)
                })
                .ToList();

            AplicarFiltros();

            PagosDelMes.Clear();

            foreach (var pago in pagos.OrderBy(p => p.FechaPago))
            {
                var socio = socios.FirstOrDefault(s => s.Id == pago.SocioId);

                PagosDelMes.Add(new PagoItem
                {
                    Pago = pago,
                    SocioTexto = socio == null ? "Socio eliminado" : $"{socio.Apellido}, {socio.Nombre}"
                });
            }

            int pagaron = _todosLosSocios.Count(s => s.CuotaPagada);
            decimal cobrado = pagos.Sum(p => p.MontoEnPesos);

            ResumenTexto = $"Cuotas pagadas: {pagaron} de {_todosLosSocios.Count} socios activos. Cobrado en el mes: ARS {cobrado:N2}";

            // Le aviso otra vez a los Picker cuál es el valor elegido, por si se quedaron en blanco
            OnPropertyChanged(nameof(FiltroEstado));
            OnPropertyChanged(nameof(FiltroCategoria));
        }

        private void ActualizarFiltroCategorias(List<Categoria> categorias)
        {
            // Guardo cuál estaba elegida porque al vaciar la lista el Picker la deselecciona
            int idElegida = FiltroCategoria?.Id ?? 0;

            CategoriasFiltro.Clear();
            CategoriasFiltro.Add(_todasLasCategorias);

            foreach (var categoria in categorias)
            {
                CategoriasFiltro.Add(categoria);
            }

            FiltroCategoria = CategoriasFiltro.FirstOrDefault(c => c.Id == idElegida) ?? _todasLasCategorias;
        }

        private void AplicarFiltros()
        {
            string texto = (Busqueda ?? string.Empty).Trim();

            var filtrados = _todosLosSocios.Where(item =>
                (texto == string.Empty
                    || item.TextoBusqueda.Contains(texto, StringComparison.OrdinalIgnoreCase))
                && (FiltroCategoria == null
                    || FiltroCategoria.Id == 0
                    || item.Socio.CategoriaId == FiltroCategoria.Id)
                && (FiltroEstado == "Todos"
                    || (FiltroEstado == "Pagaron" && item.CuotaPagada)
                    || (FiltroEstado == "Deben" && item.CuotaPendiente)));

            SociosCobranza.Clear();

            foreach (var item in filtrados)
            {
                SociosCobranza.Add(item);
            }
        }

        private async Task CambiarPeriodoAsync(int meses)
        {
            if (EstaOcupado)
            {
                return;
            }

            CerrarCobro();
            Periodo = Periodo.AddMonths(meses);

            await CargarAsync();
        }

        // ---------- Cobro ----------

        private async Task AbrirCobroAsync(SocioCobranza item, bool esActividad)
        {
            if (item == null || EstaOcupado)
            {
                return;
            }

            if (!esActividad && item.Categoria == null)
            {
                Mensaje = "El socio no tiene categoría. Asignásela en la pestaña Socios.";
                return;
            }

            if (!esActividad && item.CuotaPagada)
            {
                Mensaje = "Ese socio ya pagó la cuota de este mes.";
                return;
            }

            if (esActividad && ActividadesDisponibles.Count == 0)
            {
                Mensaje = "Primero cargá una actividad en la pestaña Precios.";
                return;
            }

            EstaOcupado = true;

            try
            {
                _ajustandoCobro = true;

                SocioElegido = item;
                EsActividad = esActividad;
                FechaPago = DateTime.Today;
                ActividadSeleccionada = null;
                UsarVale = false;
                CotizacionTexto = string.Empty;

                ValeDisponible = esActividad
                    && await _cobranzaService.ValeDisponibleAsync(item.Socio.Id, item.Categoria, Periodo);

                _ajustandoCobro = false;

                await ActualizarPrevistaAsync();

                Mensaje = "Completá el cobro (el formulario está arriba de todo).";
            }
            catch (Exception ex)
            {
                _ajustandoCobro = false;
                Mensaje = "No se pudo preparar el cobro.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        // Calcula qué precio corresponde según la fecha y la actividad elegida
        private async Task ActualizarPrevistaAsync()
        {
            _precioPrevisto = null;
            PideCotizacion = false;

            if (SocioElegido == null)
            {
                PrecioPrevistoTexto = string.Empty;
                OnPropertyChanged(nameof(TotalPesosTexto));
                return;
            }

            // Con vale no se cobra nada
            if (EsActividad && UsarVale)
            {
                PrecioPrevistoTexto = "Con vale: no se cobra.";
                OnPropertyChanged(nameof(TotalPesosTexto));
                return;
            }

            TipoConcepto tipo;
            int conceptoId;

            if (EsActividad)
            {
                if (ActividadSeleccionada == null)
                {
                    PrecioPrevistoTexto = "Elegí una actividad.";
                    OnPropertyChanged(nameof(TotalPesosTexto));
                    return;
                }

                tipo = TipoConcepto.Actividad;
                conceptoId = ActividadSeleccionada.Id;
            }
            else
            {
                tipo = TipoConcepto.Cuota;
                conceptoId = SocioElegido.Categoria?.Id ?? 0;
            }

            try
            {
                var precio = await _precioService.ObtenerPrecioAsync(tipo, conceptoId, FechaPago);

                if (precio == null)
                {
                    PrecioPrevistoTexto = "No hay un precio cargado para esa fecha.";
                    OnPropertyChanged(nameof(TotalPesosTexto));
                    return;
                }

                _precioPrevisto = precio;
                PrecioPrevistoTexto = $"Precio: {precio.Moneda} {precio.Monto:N2}";

                if (precio.Moneda == Moneda.USD)
                {
                    PideCotizacion = true;

                    // Si todavía no hay cotización cargada, intento traerla
                    if (string.IsNullOrWhiteSpace(CotizacionTexto))
                    {
                        await TraerDolarAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo calcular el precio.";
                System.Diagnostics.Debug.WriteLine(ex);
            }

            OnPropertyChanged(nameof(TotalPesosTexto));
        }

        private async Task TraerDolarAsync()
        {
            try
            {
                decimal venta = await _cotizacionService.ObtenerDolarBlueAsync();

                // Lo paso a texto con coma decimal, que es como lo lee TryParsearMonto
                CotizacionTexto = venta.ToString("0.##", CultureInfo.InvariantCulture).Replace('.', ',');
            }
            catch (CotizacionException)
            {
                Mensaje = "No se pudo traer el dólar. Escribilo a mano.";
            }
        }

        private async Task RegistrarCobroAsync()
        {
            if (EstaOcupado || SocioElegido == null)
            {
                return;
            }

            decimal cotizacion = 0;

            if (PideCotizacion && !PrecioService.TryParsearMonto(CotizacionTexto, out cotizacion))
            {
                Mensaje = "Ingresá una cotización válida, con coma para los decimales.";
                return;
            }

            EstaOcupado = true;

            try
            {
                string? error;

                if (EsActividad)
                {
                    if (ActividadSeleccionada == null)
                    {
                        Mensaje = "Elegí una actividad.";
                        return;
                    }

                    error = await _cobranzaService.RegistrarActividadAsync(
                        SocioElegido.Socio.Id,
                        SocioElegido.Categoria,
                        ActividadSeleccionada,
                        Periodo,
                        FechaPago,
                        cotizacion,
                        UsarVale);
                }
                else
                {
                    error = await _cobranzaService.RegistrarCuotaAsync(
                        SocioElegido.Socio.Id,
                        SocioElegido.Categoria!,
                        Periodo,
                        FechaPago,
                        cotizacion);
                }

                // Las reglas (cuota repetida, vale usado, sin precio) las decide el servicio
                if (error != null)
                {
                    Mensaje = error;
                    return;
                }

                CerrarCobro();
                await ActualizarDatosAsync();
                Mensaje = "Cobro registrado.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo registrar el cobro.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private void CerrarCobro()
        {
            _ajustandoCobro = true;

            SocioElegido = null;
            ActividadSeleccionada = null;
            UsarVale = false;
            ValeDisponible = false;
            CotizacionTexto = string.Empty;
            PrecioPrevistoTexto = string.Empty;
            PideCotizacion = false;
            _precioPrevisto = null;

            _ajustandoCobro = false;

            OnPropertyChanged(nameof(TotalPesosTexto));
        }

        private async Task AnularPagoAsync(PagoItem item)
        {
            if (item == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                bool confirmar = await Shell.Current.DisplayAlert(
                    "Anular pago",
                    $"¿Querés anular \"{item.Detalle}\" de {item.SocioTexto}?",
                    "Anular",
                    "Cancelar");

                if (!confirmar)
                {
                    return;
                }

                await _pagoRepositorio.EliminarAsync(item.Pago);

                await ActualizarDatosAsync();
                Mensaje = "Pago anulado.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo anular el pago.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }
    }
}