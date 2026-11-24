using System.IO;
using ClubSocial.Repositories;
using ClubSocial.Services;
using ClubSocial.ViewModels;
using ClubSocial.Views;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui;
using Microsoft.Maui.Hosting;
using Microsoft.Maui.Storage;

namespace ClubSocial
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();

            builder
                .UseMauiApp<App>()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont(
                        "OpenSans-Regular.ttf",
                        "OpenSansRegular");

                    fonts.AddFont(
                        "OpenSans-Semibold.ttf",
                        "OpenSansSemibold");
                });

            string rutaBaseDatos = Path.Combine(
                FileSystem.AppDataDirectory,
                "clubsocial.db3");

            // Persistencia y pantalla de socios.
            builder.Services.AddSingleton<ISocioRepository>(
                servicios => new SocioRepositorySQLite(rutaBaseDatos));

            builder.Services.AddSingleton<SociosViewModel>();
            builder.Services.AddSingleton<MainPage>();

            // Consulta y pantalla del clima.
            builder.Services.AddSingleton<ClimaService>(servicios => new ClimaService());
            builder.Services.AddSingleton<ClimaViewModel>();
            builder.Services.AddSingleton<ClimaPage>();

            // Categorías, precios y su pantalla.
            builder.Services.AddSingleton<ICategoriaRepository>(servicios => new CategoriaRepositorySQLite(rutaBaseDatos));
            builder.Services.AddSingleton<IPrecioRepository>(servicios => new PrecioRepositorySQLite(rutaBaseDatos));
            builder.Services.AddSingleton<PrecioService>();
            builder.Services.AddSingleton<PreciosViewModel>();
            builder.Services.AddSingleton<PreciosPage>();
            builder.Services.AddSingleton<IActividadRepository>(servicios => new ActividadRepositorySQLite(rutaBaseDatos));

            // Cobranza y su pantalla.
            builder.Services.AddSingleton<IPagoRepository>(servicios => new PagoRepositorySQLite(rutaBaseDatos));
            builder.Services.AddSingleton<ICotizacionService>(servicios => new CotizacionService());
            builder.Services.AddSingleton<CobranzaService>();
            builder.Services.AddSingleton<CobranzaViewModel>();
            builder.Services.AddSingleton<CobranzaPage>();

            // Cuentas (dónde entra y sale la plata).
            builder.Services.AddSingleton<ICuentaRepository>(servicios => new CuentaRepositorySQLite(rutaBaseDatos));
            builder.Services.AddSingleton<CuentasViewModel>();

            // Gastos y su pantalla.
            builder.Services.AddSingleton<IGastoRepository>(servicios => new GastoRepositorySQLite(rutaBaseDatos));
            builder.Services.AddSingleton<GastoService>();
            builder.Services.AddSingleton<GastosViewModel>();
            builder.Services.AddSingleton<GastosPage>();

            // Resumen mensual y su pantalla.
            builder.Services.AddSingleton<ResumenService>();
            builder.Services.AddSingleton<ResumenViewModel>();
            builder.Services.AddSingleton<ResumenPage>();

            // El detalle se crea nuevamente cada vez que lo abrimos.
            builder.Services.AddTransient<DetalleClimaViewModel>();
            builder.Services.AddTransient<DetalleClimaPage>();

            builder.Services.AddSingleton<AppShell>();

#if DEBUG
            builder.Logging.AddDebug();
#endif

            return builder.Build();
        }
    }
}