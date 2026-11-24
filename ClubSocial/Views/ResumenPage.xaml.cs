using ClubSocial.ViewModels;
using Microsoft.Maui.Controls;

namespace ClubSocial.Views
{
    public partial class ResumenPage : ContentPage
    {
        private readonly ResumenViewModel _viewModel;

        public ResumenPage(ResumenViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            BindingContext = viewModel;
        }

        // Cada vez que se abre la pestaña vuelvo a calcular el resumen
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.CargarCommand.Execute(null);
        }
    }
}