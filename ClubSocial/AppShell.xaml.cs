using ClubSocial.Views;
using Microsoft.Maui.Controls;

namespace ClubSocial
{
    public partial class AppShell : Shell
    {
        public AppShell(MainPage mainPage, ClimaPage climaPage, PreciosPage preciosPage)
        {
            InitializeComponent();

            SociosContent.Content = mainPage;
            ClimaContent.Content = climaPage;
            PreciosContent.Content = preciosPage;

            Routing.RegisterRoute(
                "detalleClima",
                typeof(DetalleClimaPage));
        }
    }
}