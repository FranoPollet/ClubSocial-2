using ClubSocial.ViewModels;
using Microsoft.Maui.Controls;

namespace ClubSocial.Views
{
    public partial class GastosPage : ContentPage
    {
        private readonly GastosViewModel _viewModel;

        public GastosPage(GastosViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        // Cada vez que se abre la pestaña actualizo los datos
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.CargarCommand.Execute(null);
        }
    }
}