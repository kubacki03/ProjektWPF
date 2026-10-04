using System.Windows;
using System.Windows.Controls;
using ProjektWPF.Services;
using ProjektWPF.ViewModels;

namespace ProjektWPF
{
    public partial class HomeView : Page
    {
        private readonly HomeViewModel _viewModel;

        public HomeView()
        {
            InitializeComponent();

            _viewModel = new HomeViewModel(new FileService(), AppServices.Navigation, AppServices.Dialogs);
            DataContext = _viewModel;

            Unloaded += (_, _) => _viewModel.Dispose();
        }

        private void ToggleSidePanel(object sender, RoutedEventArgs e)
        {
            SidePanelColumn.Width = new GridLength(SidePanelColumn.Width.Value == 0 ? 250 : 0);
        }

        private void ToggleSidePanel2(object sender, RoutedEventArgs e)
        {
            SidePanel2Column.Width = new GridLength(SidePanel2Column.Width.Value == 0 ? 250 : 0);
        }

        private void ToggleSidePanel3(object sender, RoutedEventArgs e)
        {
            _viewModel.ToggleSystemMonitorCommand.Execute(null);
            SidePanel3Column.Width = new GridLength(_viewModel.IsSystemPanelOpen ? 250 : 0);
        }
    }
}
