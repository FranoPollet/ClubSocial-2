using System;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Services;
using Microsoft.Maui.Controls;
using Microsoft.Maui.Graphics;

namespace ClubSocial.ViewModels
{
    public class ResumenViewModel : BaseViewModel
    {
        private readonly ResumenService _resumenService;

        private DateTime _periodo = CobranzaService.PrimerDiaDelMes(DateTime.Today);
        private string _ingresosTexto = string.Empty;
        private string _gastosTexto = string.Empty;
        private string _balanceTexto = string.Empty;
        private Color _balanceColor = Color.FromArgb("#203A54");
        private string _mensaje = "Cargando resumen...";
        private bool _estaOcupado;

        public ObservableCollection<ResumenCuenta> PorCuenta { get; } = new();

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

        public string IngresosTexto
        {
            get => _ingresosTexto;
            set => SetProperty(ref _ingresosTexto, value);
        }

        public string GastosTexto
        {
            get => _gastosTexto;
            set => SetProperty(ref _gastosTexto, value);
        }

        public string BalanceTexto
        {
            get => _balanceTexto;
            set => SetProperty(ref _balanceTexto, value);
        }

        // Verde si el mes cerró a favor, rojo si se gastó más de lo que entró
        public Color BalanceColor
        {
            get => _balanceColor;
            set => SetProperty(ref _balanceColor, value);
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
                    CargarCommand.ChangeCanExecute();
                    MesAnteriorCommand.ChangeCanExecute();
                    MesSiguienteCommand.ChangeCanExecute();
                }
            }
        }

        public Command CargarCommand { get; }
        public Command MesAnteriorCommand { get; }
        public Command MesSiguienteCommand { get; }

        public ResumenViewModel(ResumenService resumenService)
        {
            _resumenService = resumenService;

            CargarCommand = new Command(
                async () => await CargarAsync(),
                () => !EstaOcupado);

            MesAnteriorCommand = new Command(
                async () => await CambiarPeriodoAsync(-1),
                () => !EstaOcupado);

            MesSiguienteCommand = new Command(
                async () => await CambiarPeriodoAsync(1),
                () => !EstaOcupado);
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
                ResumenMes resumen = await _resumenService.ObtenerResumenAsync(Periodo);

                IngresosTexto = $"ARS {resumen.Ingresos:N2}";
                GastosTexto = $"ARS {resumen.Gastos:N2}";
                BalanceTexto = $"ARS {resumen.Balance:N2}";

                BalanceColor = resumen.Balance < 0
                    ? Color.FromArgb("#A83232")
                    : Color.FromArgb("#1E7B3A");

                PorCuenta.Clear();

                foreach (var cuenta in resumen.PorCuenta)
                {
                    PorCuenta.Add(cuenta);
                }

                Mensaje = resumen.Ingresos == 0 && resumen.Gastos == 0
                    ? "No hay movimientos en este mes."
                    : "Resumen del mes en pesos.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo armar el resumen.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
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
    }
}