using ClubSocial.ViewModels;
using Microsoft.Maui.Controls;

namespace ClubSocial
{
    public partial class MainPage : ContentPage
    {
        private readonly SociosViewModel _viewModel;

        public MainPage(SociosViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;

            // La pantalla toma sus datos y comandos del ViewModel.
            BindingContext = viewModel;
        }

        // Al volver a la pestaña actualizo la lista y las categorías (pudieron cambiar en Precios)
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.CargarSociosCommand.Execute(null);
        }
    }
}