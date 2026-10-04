using System.Windows.Controls;
using ProjektWPF.Services;
using ProjektWPF.ViewModels;

namespace ProjektWPF
{
    public partial class NotebookView : Page
    {
        public NotebookView()
        {
            InitializeComponent();

            DataContext = new NotebookViewModel(AppServices.Navigation, AppServices.Dialogs);
        }
    }
}
