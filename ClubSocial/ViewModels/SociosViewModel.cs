using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ClubSocial.Models;
using ClubSocial.Repositories;
using Microsoft.Maui.Controls;
using ClubSocial.Services;

namespace ClubSocial.ViewModels
{
    public class SociosViewModel : BaseViewModel
    {
        private readonly ISocioRepository _repositorio;

        // Guardamos estos datos para saber qué socio estamos editando.
        private int _idEnEdicion;
        private DateTime _fechaAltaEnEdicion = DateTime.Today;

        private string _nombre = string.Empty;
        private string _apellido = string.Empty;
        private string _dni = string.Empty;
        private string _telefono = string.Empty;
        private string _email = string.Empty;
        private string? _sectorSeleccionado;
        private bool _activo = true;
        private string _mensaje = "Presioná Cargar socios para consultar la lista.";
        private string _tituloFormulario = "Nuevo socio";
        private bool _estaOcupado;

        public ObservableCollection<Socio> Socios { get; } = new();

        public List<string> Sectores { get; } = new()
        {
            "Social",
            "Fútbol",
            "Básquet",
            "Natación"
        };

        public string Nombre
        {
            get => _nombre;
            set => SetProperty(ref _nombre, value);
        }

        public string Apellido
        {
            get => _apellido;
            set => SetProperty(ref _apellido, value);
        }

        public string Dni
        {
            get => _dni;
            set => SetProperty(ref _dni, value);
        }

        public string Telefono
        {
            get => _telefono;
            set => SetProperty(ref _telefono, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string? SectorSeleccionado
        {
            get => _sectorSeleccionado;
            set => SetProperty(ref _sectorSeleccionado, value);
        }

        public bool Activo
        {
            get => _activo;
            set => SetProperty(ref _activo, value);
        }

        public string Mensaje
        {
            get => _mensaje;
            set => SetProperty(ref _mensaje, value);
        }

        public string TituloFormulario
        {
            get => _tituloFormulario;
            set => SetProperty(ref _tituloFormulario, value);
        }

        public bool EstaOcupado
        {
            get => _estaOcupado;
            set
            {
                if (SetProperty(ref _estaOcupado, value))
                {
                    // Mientras guardamos o cargamos, evitamos otra operación.
                    CargarSociosCommand.ChangeCanExecute();
                    GuardarSocioCommand.ChangeCanExecute();
                    NuevoSocioCommand.ChangeCanExecute();
                    EditarSocioCommand.ChangeCanExecute();
                    EliminarSocioCommand.ChangeCanExecute();
                }
            }
        }

        public Command CargarSociosCommand { get; }
        public Command GuardarSocioCommand { get; }
        public Command NuevoSocioCommand { get; }
        public Command<Socio> EditarSocioCommand { get; }
        public Command<Socio> EliminarSocioCommand { get; }

        public SociosViewModel(ISocioRepository repositorio)
        {
            _repositorio = repositorio;

            CargarSociosCommand = new Command(
                async () => await CargarSociosAsync(),
                () => !EstaOcupado);

            GuardarSocioCommand = new Command(
                async () => await GuardarSocioAsync(),
                () => !EstaOcupado);

            NuevoSocioCommand = new Command(
                PrepararNuevoSocio,
                () => !EstaOcupado);

            EditarSocioCommand = new Command<Socio>(
                PrepararEdicion,
                socio => socio != null && !EstaOcupado);

            EliminarSocioCommand = new Command<Socio>(
                async socio => await EliminarSocioAsync(socio),
                socio => socio != null && !EstaOcupado);
        }

        private async Task ActualizarListaAsync()
        {
            var sociosGuardados = await _repositorio.ObtenerTodosAsync();

            Socios.Clear();

            foreach (var socio in sociosGuardados)
            {
                Socios.Add(socio);
            }
        }

        private async Task CargarSociosAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;
            Mensaje = "Cargando socios...";

            try
            {
                await ActualizarListaAsync();
                Mensaje = $"Socios registrados: {Socios.Count}.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudieron cargar los socios.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private async Task GuardarSocioAsync()
        {
            if (EstaOcupado)
            {
                return;
            }

            string nombre = (Nombre ?? string.Empty).Trim();
            string apellido = (Apellido ?? string.Empty).Trim();
            string dni = (Dni ?? string.Empty).Trim();

            // Las reglas de nombre, apellido y DNI las valida ValidadorSocio
            string? errorDatos = ValidadorSocio.Validar(nombre, apellido, dni);

            if (errorDatos != null)
            {
                Mensaje = errorDatos;
                return;
            }

            if (string.IsNullOrWhiteSpace(SectorSeleccionado))
            {
                Mensaje = "Seleccioná un sector del club.";
                return;
            }

            EstaOcupado = true;
            Mensaje = "Guardando socio...";

            try
            {
                var socioConEseDni =
                    await _repositorio.ObtenerPorDniAsync(dni);

                // Al editar, el mismo socio puede conservar su DNI.
                if (socioConEseDni != null &&
                    socioConEseDni.Id != _idEnEdicion)
                {
                    Mensaje = "Ya existe otro socio con ese DNI.";
                    return;
                }

                bool esNuevo = _idEnEdicion == 0;

                var socio = new Socio
                {
                    Id = _idEnEdicion,
                    Nombre = nombre,
                    Apellido = apellido,
                    Dni = dni,
                    Telefono = (Telefono ?? string.Empty).Trim(),
                    Email = (Email ?? string.Empty).Trim(),
                    Sector = SectorSeleccionado!,
                    FechaAlta = _fechaAltaEnEdicion,
                    Activo = Activo
                };

                int filas = await _repositorio.GuardarAsync(socio);

                if (filas == 0)
                {
                    Mensaje = "No se guardaron cambios. Cargá la lista nuevamente.";
                    return;
                }

                // Limpiamos el formulario después de guardar.
                LimpiarFormulario();

                Mensaje = esNuevo
                    ? "Socio agregado correctamente."
                    : "Socio actualizado correctamente.";

                try
                {
                    await ActualizarListaAsync();
                }
                catch (Exception ex)
                {
                    Mensaje += " No se pudo actualizar la lista; presioná Cargar socios.";
                    System.Diagnostics.Debug.WriteLine(ex);
                }
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo guardar el socio.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }

        private void PrepararNuevoSocio()
        {
            LimpiarFormulario();
            Mensaje = "Completá los datos del nuevo socio.";
        }

        private void LimpiarFormulario()
        {
            _idEnEdicion = 0;
            _fechaAltaEnEdicion = DateTime.Today;

            Nombre = string.Empty;
            Apellido = string.Empty;
            Dni = string.Empty;
            Telefono = string.Empty;
            Email = string.Empty;
            SectorSeleccionado = null;
            Activo = true;
            TituloFormulario = "Nuevo socio";
        }

        private void PrepararEdicion(Socio socio)
        {
            if (socio == null || EstaOcupado)
            {
                return;
            }

            // Copiamos los valores para no modificar la lista antes de guardar.
            _idEnEdicion = socio.Id;
            _fechaAltaEnEdicion = socio.FechaAlta;

            Nombre = socio.Nombre;
            Apellido = socio.Apellido;
            Dni = socio.Dni;
            Telefono = socio.Telefono;
            Email = socio.Email;
            SectorSeleccionado = socio.Sector;
            Activo = socio.Activo;

            TituloFormulario = "Editar socio";
            Mensaje = $"Editando a {socio.Nombre} {socio.Apellido}.";
        }

        private async Task EliminarSocioAsync(Socio socio)
        {
            if (socio == null || EstaOcupado)
            {
                return;
            }

            EstaOcupado = true;

            try
            {
                bool confirmar = await Shell.Current.DisplayAlert(
                    "Eliminar socio",
                    $"¿Querés eliminar a {socio.Nombre} {socio.Apellido}?",
                    "Eliminar",
                    "Cancelar");

                if (!confirmar)
                {
                    return;
                }

                int filas = await _repositorio.EliminarAsync(socio);

                if (filas == 0)
                {
                    Mensaje = "El socio ya no está en la base de datos.";
                    return;
                }

                // Si borramos el socio que estábamos editando, limpiamos sus datos.
                if (_idEnEdicion == socio.Id)
                {
                    LimpiarFormulario();
                }

                Socios.Remove(socio);
                Mensaje = "Socio eliminado correctamente.";
            }
            catch (Exception ex)
            {
                Mensaje = "No se pudo eliminar el socio.";
                System.Diagnostics.Debug.WriteLine(ex);
            }
            finally
            {
                EstaOcupado = false;
            }
        }
    }
}
