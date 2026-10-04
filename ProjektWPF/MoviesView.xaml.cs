using System.Windows.Controls;
using ProjektWPF.Services;
using ProjektWPF.ViewModels;

namespace ProjektWPF
{
    public partial class MoviesView : Page
    {
        public MoviesView()
        {
            InitializeComponent();

            DataContext = new MoviesViewModel(new MovieService(), AppServices.Navigation, AppServices.Dialogs);
        }
    }
}
