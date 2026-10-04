using System.Windows;
using ProjektWPF.Services;

namespace ProjektWPF
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            AppServices.Navigation = new FrameNavigationService(MainFrame);
            AppServices.Navigation.Navigate(new Welcome());
        }
    }
}
