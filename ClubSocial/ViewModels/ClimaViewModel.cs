using ClubSocial.Services;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClubSocial.Models;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Networking;

namespace ClubSocial.ViewModels
{
    public class ClimaViewModel : BaseViewModel
    {
        private readonly ClimaService _climaService;

        private bool _estaOcupado;
        private bool _estaNavegando;
        private PronosticoDia? _diaSeleccionado;

        private string _mensaje =
            "Presioná Cargar datos para consultar el pronóstico.";

        public ObservableCollection<PronosticoDia> Dias { get; } = new();

        public string Mensaje
        {
            get => _mensaje;
            set => SetProperty(ref _mensaje, value);
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
                }
            }
        }

        public Command CargarDatosCommand { get; }
        public Command VerDetalleCommand { get; }

        public ClimaViewModel(ClimaService climaService)
        {
            _climaService = climaService;

            CargarDatosCommand = new Command(
                async () => await CargarDatosAsync(),
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

                var pronostico = await _climaService.ObtenerPronosticoAsync();

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