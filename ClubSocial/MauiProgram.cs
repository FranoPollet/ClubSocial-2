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