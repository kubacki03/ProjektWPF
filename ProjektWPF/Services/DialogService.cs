using System.Windows;

namespace ProjektWPF.Services
{
    public interface IDialogService
    {
        void ShowInfo(string message, string title = "Informacja");
        void ShowWarning(string message, string title = "Błąd");
        void ShowError(string message, string title = "Błąd");
        bool Confirm(string message, string title);
    }

    public class MessageBoxDialogService : IDialogService
    {
        public void ShowInfo(string message, string title = "Informacja")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Information);
        }

        public void ShowWarning(string message, string title = "Błąd")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Warning);
        }

        public void ShowError(string message, string title = "Błąd")
        {
            MessageBox.Show(message, title, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        public bool Confirm(string message, string title)
        {
            return MessageBox.Show(message, title, MessageBoxButton.YesNo, MessageBoxImage.Question) == MessageBoxResult.Yes;
        }
    }
}
