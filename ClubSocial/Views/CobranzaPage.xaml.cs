using ClubSocial.ViewModels;
using Microsoft.Maui.Controls;

namespace ClubSocial.Views
{
    public partial class CobranzaPage : ContentPage
    {
        private readonly CobranzaViewModel _viewModel;

        public CobranzaPage(CobranzaViewModel viewModel)
        {
            InitializeComponent();

            _viewModel = viewModel;
            BindingContext = viewModel;

            // Cuando se abre el panel de cobro subo la pantalla para que se vea.
            // Mover el scroll es algo de la vista, por eso va en el code-behind y no en el ViewModel.
            viewModel.PropertyChanged += async (sender, e) =>
            {
                if (e.PropertyName == nameof(CobranzaViewModel.HayCobroAbierto) && viewModel.HayCobroAbierto)
                {
                    await Pantalla.ScrollToAsync(0, 0, true);
                }
            };
        }

        // Cada vez que se abre la pestaña actualizo los datos
        protected override void OnAppearing()
        {
            base.OnAppearing();

            _viewModel.CargarCommand.Execute(null);
        }
    }
}