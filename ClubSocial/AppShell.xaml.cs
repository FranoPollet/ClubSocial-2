using ClubSocial.Views;
using Microsoft.Maui.Controls;

namespace ClubSocial
{
    public partial class AppShell : Shell
    {
        public AppShell(
            MainPage mainPage,
            CobranzaPage cobranzaPage,
            PreciosPage preciosPage,
            ClimaPage climaPage)
        {
            InitializeComponent();

            SociosContent.Content = mainPage;
            CobranzaContent.Content = cobranzaPage;
            PreciosContent.Content = preciosPage;
            ClimaContent.Content = climaPage;

            Routing.RegisterRoute(
                "detalleClima",
                typeof(DetalleClimaPage));
        }
    }
}