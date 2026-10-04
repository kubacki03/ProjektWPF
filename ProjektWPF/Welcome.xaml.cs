using System.Windows.Controls;
using ProjektWPF.Services;
using ProjektWPF.ViewModels;

namespace ProjektWPF
{
    public partial class Welcome : Page
    {
        public Welcome()
        {
            InitializeComponent();

            DataContext = new WelcomeViewModel(new FileService(), AppServices.Navigation, AppServices.Dialogs);
        }
    }
}
