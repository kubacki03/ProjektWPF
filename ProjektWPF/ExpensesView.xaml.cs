using System.Windows.Controls;
using ProjektWPF.Services;
using ProjektWPF.ViewModels;

namespace ProjektWPF
{
    public partial class ExpensesView : Page
    {
        public ExpensesView()
        {
            InitializeComponent();

            DataContext = new ExpensesViewModel(AppServices.Navigation, AppServices.Dialogs);
        }
    }
}
