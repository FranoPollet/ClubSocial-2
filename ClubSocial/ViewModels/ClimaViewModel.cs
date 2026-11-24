using ClubSocial.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClubSocial.Models;
using Microsoft.Maui.ApplicationModel;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Networking;

namespace ClubSocial.ViewModels
{
    public class ClimaViewModel : BaseViewModel
    {
        private readonly ClimaService _climaService;

        private bool _estaOcupado;
        private bool _estaNavegando;
        private PronosticoDia? _diaSeleccionado;

        // Coordenadas con las que se consulta; arrancan en Buenos Aires
        private double _latitud = ClimaService.LatitudEjemplo;
        private double _longitud = ClimaService.LongitudEjemplo;
        private string _ubicacionTexto = "Buenos Aires · Ubicación de ejemplo";

        private string _mensaje =
            "Presioná Cargar datos para consultar el pronóstico.";

        public ObservableCollection<PronosticoDia> Dias { get; } = new();

        public string Mensaje
        {
            get => _mensaje;
            set => SetProperty(ref _mensaje, value);
        }

        public string UbicacionTexto
        {
            get => _ubicacionTexto;
            set => SetProperty(ref _ubicacionTexto, value);
        }

        public PronosticoDia? DiaSeleccionado
        {
            get => _diaSeleccionado;
            set => SetProperty(ref _diaSeleccionado, value);
        }

        public bool EstaOcupado
        {
            get => _estaOcupado;
            set
            {
                if (SetProperty(ref _estaOcupado, value))
                {
                    CargarDatosCommand.ChangeCanExecute();
                    UsarMiUbicacionCommand.ChangeCanExecute();
                }
            }
        }

        public Command CargarDatosCommand { get; }
        public Command UsarMiUbicacionCommand { get; }
        public Command VerDetalleCommand { get; }

        public ClimaViewModel(ClimaService climaService)
        {
            _climaService = climaService;

            CargarDatosCommand = new Command(
                async () => await CargarDatosAsync(),
                () => !EstaOcupado);

            UsarMiUbicacionCommand = new Command(
                async () => await UsarMiUbicacionAsync(),
                () => !EstaOcupado);

            VerDetalleCommand = new Command(
                async () => await VerDetalleAsync());
        }

        private async Task MostrarErrorAsync(string mensaje)
        {
            // Si una actualización falla, conservamos la consulta anterior.
            if (Dias.Count > 0)
            {
                mensaje += " Se conserva el pronóstico de la consulta anterior.";
            }

            Mensaje = mensaje;

            await Shell.Current.DisplayAlert(
                "Consulta del clima",
                mensaje,
                "Aceptar");
        }

        // El servicio ya clasificó el error.
        // Acá solo elegimos el texto que ve el usuario.
        private static string ObtenerMensaje(ClimaException ex)
        {
            switch (ex.Tipo)
            {
                case TipoErrorClima.SolicitudInvalida:
                    return "Error HTTP 400: los datos de la consulta no son válidos.";

                case TipoErrorClima.NoEncontrado:
                    return "Error HTTP 404: no se encontró el recurso solicitado.";

                case TipoErrorClima.ErrorServidor:
                    return "Error HTTP 500: el servidor tuvo un problema.";

                case TipoErrorClima.ErrorHttp:
                    return $"Error HTTP {ex.CodigoHttp}: " +
                           "el servicio no pudo completar la consulta.";

                case TipoErrorClima.Timeout:
                    return "La consulta demoró demasiado. Intentá nuevamente.";

                case TipoErrorClima.SinConexion:
                    return "No se pudo conectar con el servicio del clima. " +
                           "Revisá la conexión o intentá nuevamente.";

                case TipoErrorClima.FormatoInvalido:
                    return "El servicio respondió, pero los datos del pronóstico " +
                           "no tienen el formato esperado.";

                default:
                    return "Ocurrió un error inesperado al consultar el clima.";
            }
        }

        private async Task CargarDatosAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                if (Connectivity.Current.NetworkAccess != NetworkAccess.Internet)
                {
                    await MostrarErrorAsync(
                        "No hay acceso a Internet. Revisá la conexión.");

                    return;
                }

                Mensaje = "Consultando el pronóstico...";

                var pronostico = await _climaService.ObtenerPronosticoAsync(_latitud, _longitud);

                // Reemplazamos la lista cuando la consulta termina correctamente.
                Dias.Clear();

                foreach (var dia in pronostico)
                {
                    Dias.Add(dia);
                }

                Mensaje = Dias.Count == 0
                    ? "Consulta exitosa, pero no hay días disponibles."
                    : $"Se cargaron {Dias.Count} días. " +
                      $"Última consulta: {DateTime.Now:HH:mm}.";
            }
            catch (ClimaException ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                await MostrarErrorAsync(ObtenerMensaje(ex));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                await MostrarErrorAsync(
                    "Ocurrió un error inesperado al consultar el clima.");
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        // Pide el permiso, lee el GPS y después consulta el clima de ese lugar
        private async Task UsarMiUbicacionAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            bool ubicado = false;

            try
            {
                // Primero miro si ya tengo el permiso; si no, se lo pido al usuario
                PermissionStatus estado = await Permissions.CheckStatusAsync<Permissions.LocationWhenInUse>();

                if (estado != PermissionStatus.Granted)
                {
                    estado = await Permissions.RequestAsync<Permissions.LocationWhenInUse>();
                }

                if (estado != PermissionStatus.Granted)
                {
                    Mensaje = "Sin permiso de ubicación se usa Buenos Aires.";

                    // Si lo negó, ofrezco abrir los ajustes de la app para que lo active
                    bool abrirAjustes = await Shell.Current.DisplayAlert(
                        "Permiso de ubicación",
                        "Necesitamos el permiso de ubicación para consultar el clima donde estás. " +
                        "Podés activarlo en los ajustes de la aplicación.",
                        "Abrir ajustes",
                        "Ahora no");

                    if (abrirAjustes)
                    {
                        AppInfo.Current.ShowSettingsUI();
                    }

                    return;
                }

                Mensaje = "Buscando tu ubicación...";

                // La última conocida es rápida; si no hay, le pido una nueva al GPS
                Location? ubicacion = await Geolocation.Default.GetLastKnownLocationAsync();

                if (ubicacion == null)
                {
                    var pedido = new GeolocationRequest(GeolocationAccuracy.Medium, TimeSpan.FromSeconds(10));

                    ubicacion = await Geolocation.Default.GetLocationAsync(pedido);
                }

                if (ubicacion == null)
                {
                    Mensaje = "No se pudo obtener la ubicación. Se usa Buenos Aires.";
                    return;
                }

                _latitud = ubicacion.Latitude;
                _longitud = ubicacion.Longitude;

                UbicacionTexto = string.Format(
                    CultureInfo.InvariantCulture,
                    "Tu ubicación · lat {0:F4}, lon {1:F4}",
                    _latitud,
                    _longitud);

                ubicado = true;
            }
            catch (FeatureNotSupportedException)
            {
                Mensaje = "Este dispositivo no tiene GPS. Se usa Buenos Aires.";
            }
            catch (FeatureNotEnabledException)
            {
                Mensaje = "El GPS está apagado. Activá la ubicación del dispositivo.";
            }
            catch (PermissionException)
            {
                Mensaje = "Falta el permiso de ubicación.";
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                Mensaje = "No se pudo obtener la ubicación.";
            }
            finally
            {
                EstaOcupado = false;
            }

            // Recién ahora (con el botón libre) consulto el pronóstico del lugar encontrado
            if (ubicado)
            {
                await CargarDatosAsync();
            }
        }

        private async Task VerDetalleAsync()
        {
            if (_estaNavegando || DiaSeleccionado == null)
            {
                return;
            }

            _estaNavegando = true;

            var dia = DiaSeleccionado;

            // Así podemos seleccionar el mismo día después de volver.
            DiaSeleccionado = null;

            try
            {
                var parametros = new Dictionary<string, object>
                {
                    ["dia"] = dia
                };

                await Shell.Current.GoToAsync("detalleClima", parametros);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);

                Mensaje = "No se pudo abrir el detalle del día.";

                await Shell.Current.DisplayAlert(
                    "Detalle del clima",
                    Mensaje,
                    "Aceptar");
            }
            finally
            {
                _estaNavegando = false;
            }
        }
    }
}