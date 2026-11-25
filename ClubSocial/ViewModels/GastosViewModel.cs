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
using System.IO;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Media;
using Microsoft.Maui.Storage;

namespace ClubSocial.ViewModels
{
    public class GastosViewModel : BaseViewModel
    {
        private readonly IGastoRepository _gastoRepositorio;
        private readonly ICuentaRepository _cuentaRepositorio;
        private readonly GastoService _gastoService;
        private readonly ICotizacionService _cotizacionService;

        private DateTime _periodo = CobranzaService.PrimerDiaDelMes(DateTime.Today);
        private string _totalTexto = string.Empty;

        // Datos del gasto que se está cargando
        private string _descripcion = string.Empty;
        private string _montoTexto = string.Empty;
        private Moneda _monedaSeleccionada = Moneda.ARS;
        private string _cotizacionTexto = string.Empty;
        private DateTime _fechaGasto = DateTime.Today;
        private Cuenta? _cuentaSeleccionada;

        // Foto del comprobante que se está cargando (ruta del archivo copiado a la app)
        private string _rutaComprobante = string.Empty;

        public string RutaComprobante
        {
            get => _rutaComprobante;
            set
            {
                if (SetProperty(ref _rutaComprobante, value))
                {
                    OnPropertyChanged(nameof(TieneFoto));
                    OnPropertyChanged(nameof(FotoPrevia));
                }
            }
        }

        public bool TieneFoto => !string.IsNullOrEmpty(_rutaComprobante);

        public ImageSource? FotoPrevia =>
            TieneFoto ? ImageSource.FromFile(_rutaComprobante) : null;

        private string _mensaje = "Cargando gastos...";
        private bool _estaOcupado;

        public ObservableCollection<GastoItem> GastosDelMes { get; } = new();

        public ObservableCollection<Cuenta> CuentasDisponibles { get; } = new();

        public List<Moneda> Monedas { get; } = new() { Moneda.ARS, Moneda.USD };

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

        public string PeriodoTexto => FormatoFecha.MesYAnio(_periodo);

        public string TotalTexto
        {
            get => _totalTexto;
            set => SetProperty(ref _totalTexto, value);
        }

        public string Descripcion
        {
            get => _descripcion;
            set => SetProperty(ref _descripcion, value);
        }

        public string MontoTexto
        {
            get => _montoTexto;
            set => SetProperty(ref _montoTexto, value);
        }

        public Moneda MonedaSeleccionada
        {
            get => _monedaSeleccionada;
            set
            {
                if (SetProperty(ref _monedaSeleccionada, value))
                {
                    OnPropertyChanged(nameof(PideCotizacion));

                    // Si pasa a dólares y todavía no hay cotización, intento traerla
                    if (PideCotizacion && string.IsNullOrWhiteSpace(CotizacionTexto))
                    {
                        TraerDolarCommand.Execute(null);
                    }
                }
            }
        }

        // La cotización solo se pide si el gasto es en dólares
        public bool PideCotizacion => MonedaSeleccionada == Moneda.USD;

        public string CotizacionTexto
        {
            get => _cotizacionTexto;
            set => SetProperty(ref _cotizacionTexto, value);
        }

        public DateTime FechaGasto
        {
            get => _fechaGasto;
            set => SetProperty(ref _fechaGasto, value);
        }

        public Cuenta? CuentaSeleccionada
        {
            get => _cuentaSeleccionada;
            set => SetProperty(ref _cuentaSeleccionada, value);
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
                    RegistrarGastoCommand.ChangeCanExecute();
                    SacarFotoCommand.ChangeCanExecute();
                    AnularGastoCommand.ChangeCanExecute();
                }
            }
        }

        public Command CargarCommand { get; }
        public Command MesAnteriorCommand { get; }
        public Command MesSiguienteCommand { get; }
        public Command RegistrarGastoCommand { get; }
        public Command TraerDolarCommand { get; }
        public Command<GastoItem> AnularGastoCommand { get; }
        public Command SacarFotoCommand { get; }
        public Command QuitarFotoCommand { get; }

        public GastosViewModel(
            IGastoRepository gastoRepositorio,
            ICuentaRepository cuentaRepositorio,
            GastoService gastoService,
            ICotizacionService cotizacionService)
        {
            _gastoRepositorio = gastoRepositorio;
            _cuentaRepositorio = cuentaRepositorio;
            _gastoService = gastoService;
            _cotizacionService = cotizacionService;

            CargarCommand = new Command(
                async () => await CargarAsync(),
                () => !EstaOcupado);

            MesAnteriorCommand = new Command(
                async () => await CambiarPeriodoAsync(-1),
                () => !EstaOcupado);

            MesSiguienteCommand = new Command(
                async () => await CambiarPeriodoAsync(1),
                () => !EstaOcupado);

            RegistrarGastoCommand = new Command(
                async () => await RegistrarGastoAsync(),
                () => !EstaOcupado);

            TraerDolarCommand = new Command(
                async () => await TraerDolarAsync());

            SacarFotoCommand = new Command(async () => await SacarFotoAsync(),() => !EstaOcupado);

            QuitarFotoCommand = new Command(QuitarFoto);

            AnularGastoCommand = new Command<GastoItem>(
                async item => await AnularGastoAsync(item),
                item => item != null && !EstaOcupado);
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
                await ActualizarDatosAsync();

                Mensaje = CuentasDisponibles.Count == 0
                    ? "Primero cargá una cuenta en la pestaña Ajustes."
                    : "Cargá un gasto con el formulario.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudieron cargar los gastos.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task ActualizarDatosAsync()
        {
            var cuentas = await _cuentaRepositorio.ObtenerTodosAsync();
            var gastos = await _gastoRepositorio.ObtenerPorPeriodoAsync(Periodo);

            // Guardo cuál estaba elegida porque al vaciar la lista el Picker la deselecciona
            int idElegida = CuentaSeleccionada?.Id ?? 0;

            CuentasDisponibles.Clear();

            foreach (var cuenta in cuentas)
            {
                CuentasDisponibles.Add(cuenta);
            }

            CuentaSeleccionada = CuentasDisponibles.FirstOrDefault(cuenta => cuenta.Id == idElegida);

            // Le aviso otra vez al Picker cuál es la elegida, por si se quedó en blanco
            OnPropertyChanged(nameof(CuentaSeleccionada));

            GastosDelMes.Clear();

            foreach (var gasto in gastos)
            {
                var cuenta = cuentas.FirstOrDefault(c => c.Id == gasto.CuentaId);

                GastosDelMes.Add(new GastoItem
                {
                    Gasto = gasto,
                    CuentaTexto = cuenta == null ? "Sin cuenta" : cuenta.Nombre
                });
            }

            decimal total = gastos.Sum(gasto => gasto.MontoEnPesos);

            TotalTexto = $"Gastado en el mes: ARS {total:N2}";
        }

        private async Task CambiarPeriodoAsync(int meses)
        {
            if (EstaOcupado)
            {
                return;
            }

            Periodo = Periodo.AddMonths(meses);

            await CargarAsync();
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

        private async Task SacarFotoAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                if (!MediaPicker.Default.IsCaptureSupported)
                {
                    Mensaje = "Este dispositivo no tiene cámara.";
                    return;
                }

                // Primero miro si ya tengo el permiso; si no, se lo pido al usuario
                PermissionStatus estado = await Permissions.CheckStatusAsync<Permissions.Camera>();

                if (estado != PermissionStatus.Granted)
                {
                    estado = await Permissions.RequestAsync<Permissions.Camera>();
                }

                if (estado != PermissionStatus.Granted)
                {
                    Mensaje = "Sin permiso de cámara no se puede sacar la foto.";

                    bool abrirAjustes = await Shell.Current.DisplayAlert(
                        "Permiso de cámara",
                        "Necesitamos la cámara para sacar la foto del comprobante. " +
                        "Podés activarla en los ajustes de la aplicación.",
                        "Abrir ajustes",
                        "Ahora no");

                    if (abrirAjustes)
                    {
                        AppInfo.Current.ShowSettingsUI();
                    }

                    return;
                }

                FileResult? foto = await MediaPicker.Default.CapturePhotoAsync();

                // Si cerró la cámara sin sacar la foto, el resultado es null
                if (foto == null)
                {
                    Mensaje = "No se sacó ninguna foto.";
                    return;
                }

                // Copio la foto a la carpeta de la app; en la base solo guardo la ruta
                string carpeta = Path.Combine(FileSystem.AppDataDirectory, "comprobantes");
                Directory.CreateDirectory(carpeta);

                string destino = Path.Combine(carpeta, $"comprobante_{DateTime.Now:yyyyMMdd_HHmmss}.jpg");

                using (Stream origen = await foto.OpenReadAsync())
                using (FileStream salida = File.OpenWrite(destino))
                {
                    await origen.CopyToAsync(salida);
                }

                // Si ya había una foto sin registrar, la reemplazo y borro la anterior
                BorrarArchivo(RutaComprobante);

                RutaComprobante = destino;
                Mensaje = "Foto guardada. Registrá el gasto para asociarla.";
            }
            catch (FeatureNotSupportedException)
            {
                Mensaje = "Este dispositivo no tiene cámara.";
            }
            catch (PermissionException)
            {
                Mensaje = "Falta el permiso de cámara.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo sacar la foto.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private void QuitarFoto()
        {
            BorrarArchivo(RutaComprobante);
            RutaComprobante = string.Empty;
            Mensaje = "Foto quitada.";
        }

        private static void BorrarArchivo(string ruta)
        {
            if (!string.IsNullOrEmpty(ruta) && File.Exists(ruta))
            {
                File.Delete(ruta);
            }
        }

        private async Task RegistrarGastoAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            if (!PrecioService.TryParsearMonto(MontoTexto, out decimal monto))
            {
                Mensaje = "Ingresá un monto válido, sin puntos. Los decimales van con coma.";
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
                // Las reglas (descripción, monto, cuenta, cotización) las decide GastoService
                string? error = await _gastoService.RegistrarGastoAsync(
                    Descripcion,
                    monto,
                    MonedaSeleccionada,
                    cotizacion,
                    FechaGasto,
                    CuentaSeleccionada?.Id ?? 0,
                    RutaComprobante);

                if (error != null)
                {
                    Mensaje = error;
                    return;
                }

                Descripcion = string.Empty;
                MontoTexto = string.Empty;
                RutaComprobante = string.Empty;

                // Muestro el mes del gasto para que se vea en la lista
                Periodo = CobranzaService.PrimerDiaDelMes(FechaGasto);

                await ActualizarDatosAsync();
                Mensaje = $"Gasto registrado en {PeriodoTexto}.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo registrar el gasto.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task AnularGastoAsync(GastoItem item)
        {
            if (item == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                bool confirmar = await Shell.Current.DisplayAlert(
                    "Anular gasto",
                    $"¿Querés anular \"{item.Descripcion}\"?",
                    "Anular",
                    "Cancelar");

                if (!confirmar)
                {
                    return;
                }

                await _gastoRepositorio.EliminarAsync(item.Gasto);


                // Si el gasto tenía foto, borro también el archivo
                BorrarArchivo(item.Gasto.RutaComprobante);

                await ActualizarDatosAsync();
                Mensaje = "Gasto anulado.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo anular el gasto.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }
    }
}