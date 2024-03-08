using Microsoft.UI.Xaml;

namespace ALWO
{
    public partial class App : Application
    {
        public App()
        {
            this.InitializeComponent();
            WorkspaceInfoAccess.InitializeDatabase();
        }

        protected override void OnLaunched(Microsoft.UI.Xaml.LaunchActivatedEventArgs args)
        {
            MainWindow = new MainWindow();
            MainWindow.Activate();
        }

        public static Window MainWindow;
    }
}
