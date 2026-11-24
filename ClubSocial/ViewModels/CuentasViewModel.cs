using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;
using ClubSocial.Services;
using Microsoft.Maui.Controls;

namespace ClubSocial.ViewModels
{
    // Administra las cuentas del club (efectivo, bancos, etc.). Se muestra dentro de la pantalla de Ajustes.
    public class CuentasViewModel : BaseViewModel
    {
        private readonly ICuentaRepository _repositorio;

        private string _nombreCuenta = string.Empty;
        private string _mensaje = "Agregá las cuentas donde entra y sale la plata del club.";
        private bool _estaOcupado;

        public ObservableCollection<Cuenta> Lista { get; } = new();

        public string NombreCuenta
        {
            get => _nombreCuenta;
            set => SetProperty(ref _nombreCuenta, value);
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
                    GuardarCuentaCommand.ChangeCanExecute();
                    EliminarCuentaCommand.ChangeCanExecute();
                }
            }
        }

        public Command GuardarCuentaCommand { get; }
        public Command<Cuenta> EliminarCuentaCommand { get; }

        public CuentasViewModel(ICuentaRepository repositorio)
        {
            _repositorio = repositorio;

            GuardarCuentaCommand = new Command(
                async () => await GuardarCuentaAsync(),
                () => !EstaOcupado);

            EliminarCuentaCommand = new Command<Cuenta>(
                async cuenta => await EliminarCuentaAsync(cuenta),
                cuenta => cuenta != null && !EstaOcupado);
        }

        // Lo llama PreciosViewModel cuando se abre la pantalla
        public async Task CargarAsync()
        {
            var cuentas = await _repositorio.ObtenerTodosAsync();

            Lista.Clear();

            foreach (var cuenta in cuentas)
            {
                Lista.Add(cuenta);
            }
        }

        private async Task GuardarCuentaAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            // Uso el mismo validador de nombres, pasándole que se trata de una cuenta
            string? error = ValidadorCategoria.Validar(
                NombreCuenta,
                Lista.Select(cuenta => cuenta.Nombre),
                "cuenta");

            if (error != null)
            {
                Mensaje = error;
                return;
            }

            EstaOcupado = true;

            try
            {
                await _repositorio.GuardarAsync(new Cuenta { Nombre = NombreCuenta.Trim() });

                NombreCuenta = string.Empty;

                await CargarAsync();
                Mensaje = "Cuenta agregada.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo guardar la cuenta.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task EliminarCuentaAsync(Cuenta cuenta)
        {
            if (cuenta == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                bool confirmar = await Shell.Current.DisplayAlert(
                    "Eliminar cuenta",
                    $"¿Querés eliminar la cuenta {cuenta.Nombre}? Sus movimientos pasan a figurar como \"Sin cuenta\" en el resumen.",
                    "Eliminar",
                    "Cancelar");

                if (!confirmar)
                {
                    return;
                }

                await _repositorio.EliminarAsync(cuenta);

                await CargarAsync();
                Mensaje = "Cuenta eliminada.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo eliminar la cuenta.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }
    }
}