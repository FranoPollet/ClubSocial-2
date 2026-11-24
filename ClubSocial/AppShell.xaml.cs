using ClubSocial.Views;
using Microsoft.Maui.Controls;

namespace ClubSocial
{
    public partial class AppShell : Shell
    {
        public AppShell(
            MainPage mainPage,
            CobranzaPage cobranzaPage,
            GastosPage gastosPage,
            PreciosPage preciosPage,
            ClimaPage climaPage)
        {
            InitializeComponent();

            SociosContent.Content = mainPage;
            CobranzaContent.Content = cobranzaPage;
            GastosContent.Content = gastosPage;
            PreciosContent.Content = preciosPage;
            ClimaContent.Content = climaPage;

            Routing.RegisterRoute(
                "detalleClima",
                typeof(DetalleClimaPage));
        }
    }
}