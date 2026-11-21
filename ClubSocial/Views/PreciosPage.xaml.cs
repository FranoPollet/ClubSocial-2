using ClubSocial.ViewModels;
using Microsoft.Maui.Controls;

namespace ClubSocial.Views
{
    public partial class PreciosPage : ContentPage
    {
        private readonly PreciosViewModel _viewModel;

        public PreciosPage(PreciosViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        // Cada vez que se abre la pestaña actualizo la lista
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.CargarCommand.Execute(null);
        }
    }
}