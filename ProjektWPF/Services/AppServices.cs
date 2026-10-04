namespace ProjektWPF.Services
{
    public static class AppServices
    {
        public static INavigationService Navigation { get; set; } = null!;
        public static IDialogService Dialogs { get; } = new MessageBoxDialogService();
    }
}
