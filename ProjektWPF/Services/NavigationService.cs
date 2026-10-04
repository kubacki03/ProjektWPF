using System.Windows.Controls;

namespace ProjektWPF.Services
{
    public interface INavigationService
    {
        void Navigate(Page page);
        void GoBack();
    }

    public class FrameNavigationService : INavigationService
    {
        private readonly Frame _frame;

        public FrameNavigationService(Frame frame)
        {
            _frame = frame;
        }

        public void Navigate(Page page)
        {
            _frame.Navigate(page);
        }

        public void GoBack()
        {
            if (_frame.CanGoBack)
            {
                _frame.GoBack();
            }
        }
    }
}
